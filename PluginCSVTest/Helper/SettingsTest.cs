using System;
using System.Collections.Generic;
using PluginCSV.Helper;
using Xunit;

namespace PluginCSVTest.Helper
{
    public class SettingsTest
    {
        [Fact]
        public void ValidateValidTest()
        {
            // setup
            var settings = new Settings
            {
                RootPaths = new List<RootPathObject>
                {
                    new RootPathObject
                    {
                        RootPath = "../../../MockData/Data",
                        Filter = "*.csv",
                        Delimiter = ',',
                        HasHeader = true,
                        CleanupAction = "none",
                        ArchivePath = ""
                    }
                }
            };

            // act
            settings.Validate();

            // assert
        }

        [Fact]
        public void ValidateNoPathTest()
        {
            // setup
            var settings = new Settings
            {
                RootPaths = new List<RootPathObject>()
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("At least one RootPath must be defined", e.Message);
        }

        [Fact]
        public void ValidateBadRootPathTest()
        {
            // setup
            var settings = new Settings
            {
                RootPaths = new List<RootPathObject>
                {
                    new RootPathObject
                    {
                        RootPath = "../../../MockData/Data",
                        Filter = "*.csv",
                        Delimiter = ',',
                        HasHeader = true,
                        CleanupAction = "none",
                        ArchivePath = ""
                    },
                    new RootPathObject
                    {
                        RootPath = "NotADir",
                        Filter = "*.csv",
                        Delimiter = ',',
                        HasHeader = true,
                        CleanupAction = "none",
                        ArchivePath = ""
                    }
                }
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("NotADir is not a directory", e.Message);
        }

        [Fact]
        public void ValidateBadFiltersTest()
        {
            // setup
            var settings = new Settings
            {
                RootPaths = new List<RootPathObject>
                {
                    new RootPathObject
                    {
                        RootPath = "../../../MockData/Data",
                        Filter = "invalid",
                        Delimiter = ',',
                        HasHeader = true,
                        CleanupAction = "none",
                        ArchivePath = ""
                    }
                }
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("No files in given RootPaths with given Filters", e.Message);
        }
    }
}