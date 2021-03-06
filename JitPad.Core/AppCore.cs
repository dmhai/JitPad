﻿using System;
using System.IO;
using System.Reactive.Disposables;
using JitPad.Foundation;
using Reactive.Bindings.Extensions;
using System.Diagnostics;
using System.Reflection;
using JitPad.Core.Interface;
using JitPad.Core.Processor;

namespace JitPad.Core
{
    public class AppCore : NotificationObject, IDisposable
    {
        public BuildingUnit BuildingUnit { get; }

        public OpenSourceMetadata[] OpenSources => OpenSource.OpenSources;
        public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? throw new NullReferenceException();
        public string Copyright => "Copyright © 2020 Yoshihiro Ito. All rights reserved.";
        public string JitPadWebSiteUrl => "https://github.com/YoshihiroIto/JitPad";

        public readonly ICompiler Compiler;
        public readonly IDisassembler Disassembler;

        private readonly Config _config;
        private readonly CompositeDisposable _Trashes = new CompositeDisposable();

        public AppCore(Config config)
        {
            _config = config;

            var initialSourceCode = "";
            {
                try
                {
                    initialSourceCode = _config.LoadCodeTemplate();
                }
                catch
                {
                    _config.IsFileMonitoring = false;
                    _config.MonitoringFilePath = "";
                }
            }

            Compiler = new Compiler();
            Disassembler = new JitDisassembler("JitDasm/JitDasm.exe");

            BuildingUnit = new BuildingUnit(_config, Compiler, Disassembler) {SourceCode = initialSourceCode}
                .AddTo(_Trashes);

            var fileMonitor = new FileMonitor(_config).AddTo(_Trashes);
            fileMonitor.MonitoringFileChanged += (_, __) => LoadMonitoringFile();

            _config.ObserveProperty(x => x.MonitoringFilePath)
                .Subscribe(_ => LoadMonitoringFile())
                .AddTo(_Trashes);
        }

        public void Dispose()
        {
            _Trashes.Dispose();
        }

        public void OpenConfigFolder()
        {
            var dir = Path.GetDirectoryName(_config.FilePath);

            using var proc = Process.Start("explorer", $"\"{dir}\"");

            proc?.WaitForExit();
        }

        public void OpenJitPadWebSite()
        {
            OpenWeb(JitPadWebSiteUrl);
        }

        public void OpenWeb(string url)
        {
            using var proc = Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") {CreateNoWindow = true});

            proc?.WaitForExit();
        }

        public void ApplyTemplateFile()
        {
            _config.MonitoringFilePath = "";
            BuildingUnit.SourceCode = _config.LoadCodeTemplate();
        }

        public void SetMonitoringFilePath(string filePath)
        {
            if (_config.MonitoringFilePath != filePath)
                _config.MonitoringFilePath = filePath;
            else
                LoadMonitoringFile();
        }

        private void LoadMonitoringFile()
        {
            if (File.Exists(_config.MonitoringFilePath))
                BuildingUnit.SourceCode = File.ReadAllText(_config.MonitoringFilePath);
            else
                ApplyTemplateFile();
        }
    }
}