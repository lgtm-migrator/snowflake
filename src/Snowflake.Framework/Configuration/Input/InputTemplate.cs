﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;
using EnumsNET;
using Snowflake.Configuration.Attributes;
using Snowflake.Configuration.Generators;
using Snowflake.Configuration.Interceptors;
using Snowflake.Configuration.Utility;
using Snowflake.Input.Controller;
using Snowflake.Input.Controller.Mapped;
using Snowflake.Input.Device;

namespace Snowflake.Configuration.Input
{
    public class InputTemplate<T> : IInputTemplate<T>
        where T : class, IInputTemplateGeneratedProxy
    {
        /// <inheritdoc/>
        public int PlayerIndex { get; }
        public ConfigurationSectionDescriptor<T> Descriptor { get; }

        /// <inheritdoc/>
        public T Template { get; }

        /// <inheritdoc/>
        public IReadOnlyDictionary<string, DeviceCapability> Values
            => ImmutableDictionary.CreateRange(this.Template.Values);

        /// <inheritdoc/>
        IEnumerable<IInputOption> IInputTemplate.Options =>
            ImmutableList.CreateRange(this._Options.Select(p => p.Value));

        private IConfigurationSection<T> Configuration { get; }

        private IDictionary<string, IInputOption> _Options { get; }

        /// <inheritdoc/>
        public DeviceCapability this[ControllerElement virtualElement]
        {
            set
            {
                IEnumerable<string> optionKeys = from option in this._Options
                                    where option.Value.TargetElement == virtualElement
                                    where FlagEnums.HasAnyFlags(option.Value.OptionType, value.GetClass())
                                    select option.Key;

                if (!optionKeys.Any())
                {
                    throw new KeyNotFoundException(
                        "This template does not support the target element or element type.");
                }

                foreach (string optionKey in optionKeys)
                {
                    this.Template[optionKey] = value;
                }
            }
        }

        public InputTemplate(IControllerElementMappingProfile mappedElements, int playerIndex = 0)
        {
            this.PlayerIndex = playerIndex;
            this.Descriptor = new ConfigurationSectionDescriptor<T>(typeof(T).Name);

            this._Options = (from prop in typeof(T).GetProperties()
                    let inputOptionAttribute = prop.GetCustomAttribute<InputOptionAttribute>()
                    where inputOptionAttribute != null
                    let name = prop.Name
                    select (name, option: (IInputOption)new InputOption(inputOptionAttribute, name)))
                .ToDictionary(o => o.name,
                    o => o.option);
            var overrides = (from element in mappedElements
                from key in this._Options.Keys
                let option = this._Options[key]
                let target = option.TargetElement
                where element.LayoutElement == target
                where FlagEnums.HasAnyFlags(option.OptionType, element.DeviceCapability.GetClass())
                select (key, element.DeviceCapability)).ToDictionary(d => d.key, d => d.DeviceCapability);
            var map = from key in this._Options.Keys
                let value = overrides.ContainsKey(key) ? overrides[key] : DeviceCapability.None
                select new KeyValuePair<string, DeviceCapability>(key, value);
           
       
            this.ValueCollection = new ConfigurationValueCollection();
            var genInstance = typeof(T).GetCustomAttribute<ConfigurationGenerationInstanceAttribute>();
            if (genInstance == null)
                throw new InvalidOperationException("Not generated!"); // todo: mark with interface to fail at compile time.

            //var configDescriptor = new ConfigurationSectionDescriptor<T>(typeof(T).Name);
            //((ConfigurationValueCollection) this.ValueCollection).EnsureSectionDefaults(configDescriptor);

            //this.inputTemplateInterceptor = new InputTemplateInterceptor<T>(map.ToDictionary(m => m.Key, m => m.Value),
            //    this.ValueCollection,
            //    configDescriptor);
            //var circular = new InputTemplateCircularInterceptor<T>(this);

            this.Template =
              (T)Instantiate.CreateInstance(genInstance.InstanceType,
                  new[] { typeof(IConfigurationSectionDescriptor), typeof(IConfigurationValueCollection), typeof(Dictionary<string, DeviceCapability>) },
                  Expression.Constant(this.Descriptor), Expression.Constant(this.ValueCollection), 
                  Expression.Constant(map.ToDictionary(m => m.Key, m => m.Value)));
            this.Configuration = new ConfigurationSection<T>(this.ValueCollection, this.Descriptor, this.Template);

            //this.Template = generator.CreateInterfaceProxyWithoutTarget<T>(circular, this.inputTemplateInterceptor);
        }

        /// <inheritdoc/>
        IReadOnlyDictionary<string, IConfigurationValue> IConfigurationSection.Values => this.Configuration.Values;

        /// <inheritdoc/>
        IConfigurationSectionDescriptor IConfigurationSection.Descriptor => this.Configuration.Descriptor;

        /// <inheritdoc/>
        object? IConfigurationSection.this[string key]
        {
            get { return this.Configuration[key]; }
            set { this.Configuration[key] = value; }
        }

        /// <inheritdoc/>
        T IConfigurationSection<T>.Configuration => this.Configuration.Configuration;

        public IConfigurationValueCollection ValueCollection { get; }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<IConfigurationOptionDescriptor, IConfigurationValue>> GetEnumerator()
        {
            return this.Configuration.Descriptor.Options
                .Select(o =>
                    new KeyValuePair<IConfigurationOptionDescriptor, IConfigurationValue>(o, 
                    this.Configuration.Values[o.OptionKey]))
                .GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
