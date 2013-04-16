// -----------------------------------------------------------------------
// <copyright file="TasslehoffRunner.cs" company="-">
// Copyright (c) 2013 larukedi (eser@sent.com). All rights reserved.
// </copyright>
// <author>larukedi (http://github.com/larukedi/)</author>
// -----------------------------------------------------------------------

//// This program is free software: you can redistribute it and/or modify
//// it under the terms of the GNU General Public License as published by
//// the Free Software Foundation, either version 3 of the License, or
//// (at your option) any later version.
//// 
//// This program is distributed in the hope that it will be useful,
//// but WITHOUT ANY WARRANTY; without even the implied warranty of
//// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//// GNU General Public License for more details.
////
//// You should have received a copy of the GNU General Public License
//// along with this program.  If not, see <http://www.gnu.org/licenses/>.

namespace Tasslehoff.Runner
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using Tasslehoff.Globals;
    using Tasslehoff.Library.Config;
    using Tasslehoff.Library.Cron;
    using Tasslehoff.Library.DataAccess;
    using Tasslehoff.Library.Extensions;
    using Tasslehoff.Library.Plugins;
    using Tasslehoff.Library.Services;
    using Tasslehoff.Library.Utils;
    using Tasslehoff.Library.WebServices;
    using Tasslehoff.Runner.Memcached;
    using Tasslehoff.Runner.RabbitMQ;

    /// <summary>
    /// TasslehoffRunner class.
    /// </summary>
    public class TasslehoffRunner : ServiceContainer
    {
        // constants

        /// <summary>
        /// Filename of the default configuration file
        /// </summary>
        public const string ConfigFilename = "instanceConfig.json"; 

        // fields

        /// <summary>
        /// Singleton instance
        /// </summary>
        private static TasslehoffRunner instance = null;

        /// <summary>
        /// The options
        /// </summary>
        private readonly RunnerOptions options;

        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly RunnerConfig configuration;

        /// <summary>
        /// The database
        /// </summary>
        private readonly Database database;

        /// <summary>
        /// The cron manager
        /// </summary>
        private readonly CronManager cronManager;

        /// <summary>
        /// The extension manager
        /// </summary>
        private readonly ExtensionManager extensionManager;

        /// <summary>
        /// The plugin container
        /// </summary>
        private readonly PluginContainer pluginContainer;

        /// <summary>
        /// The web service manager
        /// </summary>
        private readonly WebServiceManager webServiceManager;

        /// <summary>
        /// The message queue
        /// </summary>
        private RabbitMQConnection messageQueue = null;

        /// <summary>
        /// The cache
        /// </summary>
        private MemcachedConnection cache = null;

        // constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TasslehoffRunner" /> class.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="configuration">The configuration.</param>
        internal TasslehoffRunner(RunnerOptions options, RunnerConfig configuration) : base()
        {
            // singleton pattern
            if (TasslehoffRunner.instance == null)
            {
                TasslehoffRunner.instance = this;
            }

            // initialization
            this.options = options;
            this.configuration = configuration;
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(configuration.Culture);

            this.database = new Database(this.configuration.DatabaseDriver, this.configuration.DatabaseConnectionString);

            RabbitMQConnection.Address = configuration.RabbitMQAddress;

            this.cronManager = new CronManager();
            this.AddChild(this.cronManager);

            this.extensionManager = new ExtensionManager();
            this.AddChild(this.extensionManager);

            // search for extensions
            string extensionDirectory = Path.Combine(this.options.WorkingDirectory, "extensions");
            if (Directory.Exists(extensionDirectory))
            {
                this.extensionManager.SearchFiles(extensionDirectory + "\\*.dll");
            }
            
            this.pluginContainer = new PluginContainer(this.extensionManager);
            this.AddChild(this.pluginContainer);

            this.webServiceManager = new WebServiceManager(this.configuration.WebServiceEndpoint);
            this.AddChild(this.webServiceManager);
        }

        // properties

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>
        /// The singleton instance.
        /// </value>
        public static TasslehoffRunner Instance
        {
            get
            {
                return TasslehoffRunner.instance;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name
        {
            get
            {
                return "Tasslehoff Runner";
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public override string Description
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>
        /// The options.
        /// </value>
        public RunnerOptions Options
        {
            get
            {
                return this.options;
            }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public RunnerConfig Configuration
        {
            get
            {
                return this.configuration;
            }
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public Database Database
        {
            get
            {
                return this.database;
            }
        }

        /// <summary>
        /// Gets the cron manager.
        /// </summary>
        /// <value>
        /// The cron manager.
        /// </value>
        public CronManager CronManager
        {
            get
            {
                return this.cronManager;
            }
        }

        /// <summary>
        /// Gets the extension manager.
        /// </summary>
        /// <value>
        /// The extension manager.
        /// </value>
        public ExtensionManager ExtensionManager
        {
            get
            {
                return this.extensionManager;
            }
        }

        /// <summary>
        /// Gets the plugin container.
        /// </summary>
        /// <value>
        /// The plugin container.
        /// </value>
        public PluginContainer PluginContainer
        {
            get
            {
                return this.pluginContainer;
            }
        }

        /// <summary>
        /// Gets the web service manager.
        /// </summary>
        /// <value>
        /// The web service manager.
        /// </value>
        public WebServiceManager WebServiceManager
        {
            get
            {
                return this.webServiceManager;
            }
        }

        /// <summary>
        /// Gets the message queue.
        /// </summary>
        /// <value>
        /// The message queue.
        /// </value>
        public RabbitMQConnection MessageQueue
        {
            get
            {
                return this.messageQueue;
            }
        }

        /// <summary>
        /// Gets the cache.
        /// </summary>
        /// <value>
        /// The cache.
        /// </value>
        public MemcachedConnection Cache
        {
            get
            {
                return this.cache;
            }
        }

        // methods

        /// <summary>
        /// Writes the header.
        /// </summary>
        /// <param name="output">The output.</param>
        public static void WriteHeader(TextWriter output)
        {
            output.WriteLine("Tasslehoff 1.0  (c) 2013 larukedi (eser@sent.com). All rights reserved.");
            output.WriteLine("This program is free software under the terms of the GPL v3 or later.");
            output.WriteLine();
        }

        /// <summary>
        /// Creates the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="output">The output.</param>
        /// <returns>
        /// Created runner.
        /// </returns>
        public static TasslehoffRunner Create(RunnerOptions options, TextWriter output)
        {
            TasslehoffRunner.WriteHeader(output);

            // working directory
            string workingDirectory = options.WorkingDirectory ?? ".";

            if (!Path.IsPathRooted(workingDirectory))
            {
                workingDirectory = Path.Combine(Environment.CurrentDirectory, workingDirectory);
            }

            if (!Directory.Exists(workingDirectory))
            {
                throw new ArgumentException("Working directory not found or inaccessible - \"" + workingDirectory + "\".", "--working-dir");
            }

            options.WorkingDirectory = workingDirectory;

            // config file
            string configFile = options.ConfigFile ?? Path.Combine(workingDirectory, TasslehoffRunner.ConfigFilename);

            RunnerConfig config;
            if (File.Exists(configFile))
            {
                Stream fileStream = File.OpenRead(configFile);
                config = ConfigSerializer.Load<RunnerConfig>(fileStream);
            }
            else if (options.ConfigFile == null)
            {
                config = new RunnerConfig();
                ConfigSerializer.Reset(config);
                ConfigSerializer.Save(File.OpenWrite(configFile), config);
            }
            else
            {
                throw new ArgumentException("File not found or inaccessible - \"" + configFile + "\".", "--config");
            }

            options.ConfigFile = configFile;

            // help
            bool showHelp = options.ShowHelp;

            if (showHelp)
            {
                output.Write(RunnerOptions.Help());
                return null;
            }

            return new TasslehoffRunner(options, config);
        }

        /// <summary>
        /// Services the start.
        /// </summary>
        protected override void ServiceStart()
        {
            this.messageQueue = new RabbitMQConnection();

            string[] memcachedAddresses = !string.IsNullOrWhiteSpace(this.configuration.MemcachedAddresses) ? this.configuration.MemcachedAddresses.Split(',') : new string[0];
            this.cache = new MemcachedConnection(memcachedAddresses);
        }

        /// <summary>
        /// Services the stop.
        /// </summary>
        protected override void ServiceStop()
        {
            this.cronManager.Clear();

            VariableUtils.CheckAndDispose(this.cache);
            this.cache = null;

            VariableUtils.CheckAndDispose(this.messageQueue);
            this.messageQueue = null;
        }

        /// <summary>
        /// Called when [dispose].
        /// </summary>
        protected override void OnDispose()
        {
            base.OnDispose();

            VariableUtils.CheckAndDispose(this.cache);
            this.cache = null;

            VariableUtils.CheckAndDispose(this.messageQueue);
            this.messageQueue = null;
        }
    }
}