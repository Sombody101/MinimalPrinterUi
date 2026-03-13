using System.ComponentModel;
using System.Reflection;
using System.Text;
using EnvPropPair = (SimpleFluidd.Configuration.EnvironmentVarAttribute, System.Reflection.PropertyInfo);

namespace SimpleFluidd.Configuration;

public static class EnvironmentVariableMapper
{
    public static TConfig MapTo<TConfig>() where TConfig : class
    {
        IEnumerable<EnvPropPair> applicableProps = GetApplicableProperties<TConfig>();

        if (!applicableProps.Any())
        {
            return Activator.CreateInstance<TConfig>();
        }

        return InitializeProperties<TConfig>(applicableProps);
    }

    private static TConfig InitializeProperties<TConfig>(IEnumerable<EnvPropPair> properties)
    {
        var mapFailures = new List<MapFailure>();

        TConfig config = Activator.CreateInstance<TConfig>();

        foreach ((var attr, var prop) in properties)
        {
            var envVarValue = Environment.GetEnvironmentVariable(attr.VariableName);

            if (string.IsNullOrWhiteSpace(envVarValue))
            {
                DefaultValueAttribute? defaultValue = prop.GetCustomAttribute<DefaultValueAttribute>();
                if (defaultValue is not null)
                {
                    prop.SetValue(config, defaultValue.Value);
                    continue;
                }

                if (!attr.Required)
                {
                    continue;
                }

                mapFailures.Add(new()
                {
                    Reason = MapFailureReason.VariableNotSet,
                    SplitMessage = [attr.VariableName],
                });

                continue;
            }

            try
            {
                var convertedValue = Convert.ChangeType(envVarValue, prop.PropertyType);
                prop.SetValue(config, convertedValue);
            }
            catch (FormatException fex)
            {
                mapFailures.Add(new()
                {
                    Reason = MapFailureReason.BadFormat,
                    SplitMessage = [attr.VariableName, " - ", fex.Message],
                });
            }
        }

        if (mapFailures.Count > 0)
        {
            StringBuilder errorBuffer = new();

            foreach (var failure in mapFailures)
            {
                errorBuffer.Append("\n\t")
                    .Append(MapFailureReasonToString(failure.Reason))
                    .Append(": ").AppendLine(failure.GetMessage());
            }

            var error = $"Invalid Environment Variables: {errorBuffer}";
            Console.WriteLine(error, Console.Error);
            throw new Exception(error)!;
        }

        return config;
    }

    private static IEnumerable<EnvPropPair> GetApplicableProperties<T>()
    {
        return typeof(T).GetProperties()
            .Select(p => (attribute: p.GetCustomAttribute<EnvironmentVarAttribute>(), property: p))
            .Where(p => p.attribute is not null)!;
    }

    private static string MapFailureReasonToString(MapFailureReason reason)
    {
        return reason switch
        {
            MapFailureReason.VariableNotSet => "Missing",
            MapFailureReason.BadFormat => "Bad format",
            _ => reason.ToString()
        };
    }

    private enum MapFailureReason
    {
        VariableNotSet,
        BadFormat, // For numbers
    }

    private readonly struct MapFailure
    {
        public readonly MapFailureReason Reason { get; init; }

        public readonly string[] SplitMessage { get; init; }

        public readonly string GetMessage()
        {
            return string.Concat(SplitMessage);
        }
    }
}
