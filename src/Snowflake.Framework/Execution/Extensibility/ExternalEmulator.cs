﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Snowflake.Configuration;
using Snowflake.Extensibility.Provisioning;
using Snowflake.Model.Records.Game;
using Snowflake.Services;

namespace Snowflake.Execution.Extensibility
{
    /// <summary>
    /// Represents a <see cref="IEmulator"/> that wraps an external emulator.
    /// </summary>
    public abstract class ExternalEmulator : ProvisionedPlugin, IEmulator
    {
        protected ExternalEmulator(IPluginProvision provision,
            IStoneProvider stone)
            : base(provision)
        {
            this.StoneProvider = stone;
            this.Properties = new EmulatorProperties(provision, stone);
        }

        /// <summary>
        /// Gets a <see cref="IStoneProvider"/>. We noticed that many emulator wrappers require
        /// access to platform and controller information through Stone.
        /// </summary>
        protected IStoneProvider StoneProvider { get; }

        /// <inheritdoc/>
        public abstract IEmulatorTaskRunner Runner { get; }

        /// <inheritdoc/>
        public IEmulatorProperties Properties { get; }

        /// <inheritdoc/>
        public abstract IEmulatorTask CreateTask(IGameRecord executingGame,
            IList<IEmulatedController> controllerConfiguration,
            string profileContext = "default");
    }
}
