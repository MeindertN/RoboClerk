using NUnit.Framework;
using NSubstitute;
using RoboClerk;
using System;
using System.IO;
using System.IO.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using RoboClerk.Core;
using System.Reflection;

namespace RoboClerk.Tests
{
    [TestFixture]
    public class TestPluginLoader
    {
        private IFileSystem _fileSystem;
        private IFileProviderPlugin _fileProviderPlugin;
        private IPluginAssemblyLoader _assemblyLoader;
        private PluginLoader _pluginLoader;
        private string _pluginDir = @"c:\plugins";

        [SetUp]
        public void Setup()
        {
            _fileSystem = Substitute.For<IFileSystem>();
            _fileProviderPlugin = Substitute.For<IFileProviderPlugin>();
            _assemblyLoader = Substitute.For<IPluginAssemblyLoader>();
            _pluginLoader = new PluginLoader(_fileSystem, _fileProviderPlugin, _assemblyLoader);
        }

        [UnitTestAttribute(
            Identifier = "41f44c9d-bf2d-46a8-95a8-c60e6182dacd",
            Purpose = "Test LoadAll throws DirectoryNotFoundException when plugin directory does not exist",
            PostCondition = "DirectoryNotFoundException is thrown")]
        [Test]
        public void LoadAll_DirectoryNotFound_ThrowsException()
        {
            _fileSystem.Directory.Exists(_pluginDir).Returns(false);
            Assert.Throws<DirectoryNotFoundException>(() => _pluginLoader.LoadAll<IPlugin>(_pluginDir));
        }

        [UnitTestAttribute(
            Identifier = "4e7f3684-ab20-4749-bb01-5d3f0704e2b5",
            Purpose = "Test LoadAll returns configured ServiceProvider when directory is empty",
            PostCondition = "ServiceProvider is returned with configured globals and file provider")]
        [Test]
        public void LoadAll_EmptyDirectory_ReturnsServiceProviderWithDefaults()
        {
            _fileSystem.Directory.Exists(_pluginDir).Returns(true);
            _assemblyLoader.LoadFromDirectory(_pluginDir).Returns(new List<Assembly>());

            bool configured = false;
            var provider = _pluginLoader.LoadAll<IPlugin>(_pluginDir, services => {
                configured = true;
                services.AddSingleton<string>("test");
            });

            Assert.That(configured, Is.True);
            Assert.That(provider.GetService<IFileProviderPlugin>(), Is.Not.Null);
            Assert.That(provider.GetService<string>(), Is.EqualTo("test"));
        }

        [UnitTestAttribute(
            Identifier = "a1632430-28ad-4a0f-8516-dd0e452b907c",
            Purpose = "Test LoadByName throws DirectoryNotFoundException when plugin directory does not exist",
            PostCondition = "DirectoryNotFoundException is thrown")]
        [Test]
        public void LoadByName_DirectoryNotFound_ThrowsException()
        {
            _fileSystem.Directory.Exists(_pluginDir).Returns(false);
            Assert.Throws<DirectoryNotFoundException>(() => _pluginLoader.LoadByName<IPlugin>(_pluginDir, "MyPlugin"));
        }

        [UnitTestAttribute(
            Identifier = "b5c21913-cf63-4859-ac32-cbd94e1ef7f2",
            Purpose = "Test LoadByName returns null when plugin is not found",
            PostCondition = "Returns null")]
        [Test]
        public void LoadByName_PluginNotFound_ReturnsNull()
        {
            _fileSystem.Directory.Exists(_pluginDir).Returns(true);
            _assemblyLoader.LoadFromDirectory(_pluginDir).Returns(new List<Assembly>());

            var result = _pluginLoader.LoadByName<IPlugin>(_pluginDir, "MyPlugin");
            Assert.That(result, Is.Null);
        }
    }
}
