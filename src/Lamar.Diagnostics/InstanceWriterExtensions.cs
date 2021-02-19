using System.Linq;
using System.Text;
using Baseline;
using Lamar.IoC.Diagnostics;
using Lamar.IoC.Instances;
using LamarCodeGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spectre.Console;

namespace Lamar.Diagnostics
{
    public static class InstanceWriterExtensions
    {
        public static void WriteBuildPlanNode(this TreeNode parent, LamarServicesInput input,
            Instance instance, bool isDefault, string prefix = null)
        {
            if (instance is ConstructorInstance c)
            {
                parent.WriteConstructorBuildPlan(input, c, isDefault, prefix);
            }
            else
            {
                parent.WriteSingleInstanceNode(input, instance, isDefault, prefix);
            }
        }

        public static void WriteConstructorBuildPlan(this TreeNode parent, LamarServicesInput input,
            ConstructorInstance instance, bool isDefault, string prefix = null)
        {
            var top = parent.WriteSingleInstanceNode(input, instance, isDefault, prefix);

            foreach (var argument in instance.Arguments)
            {
                string argumentPrefix = $"[blue]{argument.Parameter.Name}[/]: ";
                if (argument.Instance.IsInlineDependency())
                {
                    top.WriteBuildPlanNode(input, argument.Instance, false, argumentPrefix);
                }

                switch (argument.Instance.Lifetime)
                {
                    case ServiceLifetime.Transient:
                        top.WriteBuildPlanNode(input, argument.Instance, false, argumentPrefix );
                        break;
                    
                    case ServiceLifetime.Scoped:
                        top.AddNode($"{argumentPrefix} = Resolved from Scope -> {argument.Instance.ToDescription()}");
                        break;
                    
                    case ServiceLifetime.Singleton:
                        top.AddNode($"{argumentPrefix} = Singleton Resolved from Root -> {argument.Instance.ToDescription()}");
                        break;
                }
            }

            foreach (var setter in instance.Setters)
            {
                var setterPrefix = $"[blue]Set {setter.Property.Name} = [/]";
                top.WriteBuildPlanNode(input, setter.Instance, false, setterPrefix);
            }
            
            // setters
        }
        
        public static TreeNode WriteSingleInstanceNode(this TreeNode parent, LamarServicesInput input, Instance instance,
            bool isDefault, string prefix = null)
        {
            prefix ??= $"[blue]{instance.Lifetime}:[/] ";

            var description = $"{prefix}{instance.ToDescription()}";
            if (input.VerboseFlag && !instance.IsInlineDependency())
            {
                description += $" named '{instance.Name}'";
            }

            if (isDefault)
            {
                description = description.BoldText();
            }

            return parent.AddNode(description);
        }
        
        public static void WriteInstancesTable(this TreeNode parent, IServiceFamilyConfiguration configuration,
            WhatDoIHaveDisplay displayMode)
        {
            var table = new Table();
            table.AddColumns("Name", "Description", "Lifetime");
            foreach (var instance in configuration.Instances)
            {
                if (configuration.Default.Instance.Equals(instance.Instance))
                {
                    table.AddRow((instance.Name+ " (Default)").BoldText() , instance.Instance.ToDescription().BoldText(),
                        instance.Lifetime.BoldText());
                }
                else
                {
                    table.AddRow(instance.Name, instance.Instance.ToDescription(),
                        instance.Lifetime.ToString());
                }
            }

            parent.AddNode(table);
        }
        
        public static string ToDescription(this Instance instance)
        {
            if (instance.ServiceType.IsOption(out var optionType))
            {
                if (instance.ImplementationType.Closes(typeof(OptionsManager<>)))
                {
                    return $"IOptions<{optionType.FullNameInCode()}>";
                }
            }
            
            if (instance.ServiceType.IsLogger(out var loggedType))
            {
                if (instance.ImplementationType.Closes(typeof(Logger<>)))
                {
                    return $"ILogger<{loggedType.FullNameInCode()}>";
                }
            }

            if (instance is ObjectInstance o)
            {
                return "User Supplied: " + o.Service?.ToString() ?? "Null";
            }

            if (instance is ConstructorInstance c)
            {
                string text = $"new {c.ImplementationType.CleanFullName()}()";
            
                if (c.Constructor != null)
                {
                    text = $"new {c.ImplementationType.CleanFullName()}({c.Constructor.GetParameters().Select(x => x.Name).Join(", ")})";
                }

                return text;
            }

            return instance.ToString();
        }

    }
}