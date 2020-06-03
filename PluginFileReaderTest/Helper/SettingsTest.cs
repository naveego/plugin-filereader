using System;
using System.Collections.Generic;
using PluginFileReader.Helper;
using Xunit;

namespace PluginFileReaderTest.Helper
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
                        RootPath = "../../../MockData/DelimitedData",
                        Filter = "*.csv",
                        Mode = "Delimited",
                        Delimiter = ",",
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

        // [Fact]
        // public void ValidateNoPathTest()
        // {
        //     // setup
        //     var settings = new Settings
        //     {
        //         RootPaths = new List<RootPathObject>()
        //     };
        //
        //     // act
        //     Exception e = Assert.Throws<Exception>(() => settings.Validate());
        //
        //     // assert
        //     Assert.Equal("", e.Message);
        // }

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
                        RootPath = "../../../MockData/DelimitedData",
                        Mode = "Delimited",
                        Filter = "*.csv",
                        Delimiter = ",",
                        HasHeader = true,
                        CleanupAction = "none",
                        ArchivePath = ""
                    },
                    new RootPathObject
                    {
                        RootPath = "NotADir",
                        Mode = "Delimited",
                        Filter = "*.csv",
                        Delimiter = ",",
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
        public void ValidateNoModeTest()
        {
            // setup
            var settings = new Settings
            {
                RootPaths = new List<RootPathObject>
                {
                    new RootPathObject
                    {
                        RootPath = "../../../MockData/DelimitedData",
                        Filter = "*.csv",
                        Delimiter = ",",
                        HasHeader = true,
                        CleanupAction = "none",
                        ArchivePath = ""
                    }
                }
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("../../../MockData/DelimitedData does not have a Mode set", e.Message);
        }
        
        [Fact]
        public void ValidateNoColumnsFixedWidthColumnsTest()
        {
            // setup
            var settings = new Settings
            {
                RootPaths = new List<RootPathObject>
                {
                    new RootPathObject
                    {
                        RootPath = "../../../MockData/DelimitedData",
                        Filter = "*.csv",
                        Mode = "Fixed Width Columns",
                        Delimiter = ",",
                        HasHeader = true,
                        CleanupAction = "none",
                        ArchivePath = "",
                        Columns = new List<Column>()
                    }
                }
            };

            // act
            Exception e = Assert.Throws<Exception>(() => settings.Validate());

            // assert
            Assert.Contains("../../../MockData/DelimitedData is set to Fixed Width Columns and has no Columns defined", e.Message);
        }
    }
}