﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Umbraco.Cms.Core.Collections;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.Install.Models;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Extensions;

namespace Umbraco.Cms.Core.Install
{
    /// <summary>
    /// An internal in-memory status tracker for the current installation
    /// </summary>
    public class InstallStatusTracker
    {
        private readonly IHostingEnvironment _hostingEnvironment;
        private readonly IJsonSerializer _jsonSerializer;

        public InstallStatusTracker(IHostingEnvironment hostingEnvironment, IJsonSerializer jsonSerializer)
        {
            _hostingEnvironment = hostingEnvironment;
            _jsonSerializer = jsonSerializer;
        }

        private static ConcurrentHashSet<InstallTrackingItem> _steps = new ConcurrentHashSet<InstallTrackingItem>();

        private string GetFile(Guid installId)
        {
            var file = _hostingEnvironment.MapPathContentRoot(Constants.SystemDirectories.TempData.EnsureEndsWith('/') + "Install/"
                                                                                                  + "install_"
                                                                                                  + installId.ToString("N")
                                                                                                  + ".txt");
            return file;
        }

        public void Reset()
        {
            _steps = new ConcurrentHashSet<InstallTrackingItem>();
            ClearFiles();
        }

        public void ClearFiles()
        {
            var dir = _hostingEnvironment.MapPathContentRoot(Constants.SystemDirectories.TempData.EnsureEndsWith('/') + "Install/");
            if (Directory.Exists(dir))
            {
                var files = Directory.GetFiles(dir);
                foreach (var f in files)
                {
                    File.Delete(f);
                }
            }
            else
            {
                Directory.CreateDirectory(dir);
            }
        }

        public IEnumerable<InstallTrackingItem> InitializeFromFile(Guid installId)
        {
            //check if we have our persisted file and read it
            var file = GetFile(installId);
            if (File.Exists(file))
            {
                var deserialized = _jsonSerializer.Deserialize<IEnumerable<InstallTrackingItem>>(
                    File.ReadAllText(file));
                foreach (var item in deserialized)
                {
                    _steps.Add(item);
                }
            }
            else
            {
                throw new InvalidOperationException("Cannot initialize from file, the installation file with id " + installId + " does not exist");
            }
            return new List<InstallTrackingItem>(_steps);
        }

        public IEnumerable<InstallTrackingItem> Initialize(Guid installId, IEnumerable<InstallSetupStep> steps)
        {
            //if there are no steps in memory
            if (_steps.Count == 0)
            {
                //check if we have our persisted file and read it
                var file = GetFile(installId);
                if (File.Exists(file))
                {
                    var deserialized = _jsonSerializer.Deserialize<IEnumerable<InstallTrackingItem>>(
                        File.ReadAllText(file));
                    foreach (var item in deserialized)
                    {
                        _steps.Add(item);
                    }
                }
                else
                {
                    ClearFiles();

                    //otherwise just create the steps in memory (brand new install)
                    foreach (var step in steps.OrderBy(x => x.ServerOrder))
                    {
                        _steps.Add(new InstallTrackingItem(step.Name, step.ServerOrder));
                    }
                    //save the file
                    var serialized = _jsonSerializer.Serialize(new List<InstallTrackingItem>(_steps));
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllText(file, serialized);
                }
            }
            else
            {
                //ensure that the file exists with the current install id
                var file = GetFile(installId);
                if (File.Exists(file) == false)
                {
                    ClearFiles();

                    //save the correct file
                    var serialized = _jsonSerializer.Serialize(new List<InstallTrackingItem>(_steps));
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    File.WriteAllText(file, serialized);
                }
            }

            return new List<InstallTrackingItem>(_steps);
        }

        public void SetComplete(Guid installId, string name, IDictionary<string, object> additionalData = null)
        {
            var trackingItem = _steps.Single(x => x.Name == name);
            if (additionalData != null)
            {
                trackingItem.AdditionalData = additionalData;
            }
            trackingItem.IsComplete = true;

            //save the file
            var file = GetFile(installId);
            var serialized = _jsonSerializer.Serialize(new List<InstallTrackingItem>(_steps));
            File.WriteAllText(file, serialized);
        }

        public static IEnumerable<InstallTrackingItem> GetStatus()
        {
            return new List<InstallTrackingItem>(_steps).OrderBy(x => x.ServerOrder);
        }
    }
}
