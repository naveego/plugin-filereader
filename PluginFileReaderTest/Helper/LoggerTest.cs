using System;
using System.IO;
using System.Linq;
using PluginFileReader.Helper;
using Xunit;

namespace PluginFileReaderTest.Helper
{
    public class LoggerTest
    {
        private static string _logDirectory = "logs";

        [Fact]
        public void VerboseTest()
        {
            var files = Directory.GetFiles(_logDirectory);
            
            // setup
            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }

            Logger.Init();
            Logger.SetLogLevel(Logger.LogLevel.Verbose);

            // act
            Logger.Verbose("verbose");
            Logger.Debug("debug");
            Logger.Info("info");
            Logger.Error(new Exception("error"), "error");
            Logger.CloseAndFlush();

            // assert
            files = Directory.GetFiles(_logDirectory);
            Assert.Single(files);
            
            string[] lines = File.ReadAllLines(files.First());

            Assert.Equal(5, lines.Length);

            // cleanup
            File.Delete(files.First());
        }

        [Fact]
        public void DebugTest()
        {
            var files = Directory.GetFiles(_logDirectory);
            
            // setup
            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }

            Logger.Init();
            Logger.SetLogLevel(Logger.LogLevel.Debug);

            // act
            Logger.Verbose("verbose");
            Logger.Debug("debug");
            Logger.Info("info");
            Logger.Error(new Exception("error"), "error");
            Logger.CloseAndFlush();

            // assert
            files = Directory.GetFiles(_logDirectory);
            Assert.Single(files);
            
            string[] lines = File.ReadAllLines(files.First());

            Assert.Equal(4, lines.Length);

            // cleanup
            File.Delete(files.First());
        }

        [Fact]
        public void InfoTest()
        {
            var files = Directory.GetFiles(_logDirectory);
            
            // setup
            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }

            Logger.Init();
            Logger.SetLogLevel(Logger.LogLevel.Info);

            // act
            Logger.Verbose("verbose");
            Logger.Debug("debug");
            Logger.Info("info");
            Logger.Error(new Exception("error"), "error");
            Logger.CloseAndFlush();

            // assert
            files = Directory.GetFiles(_logDirectory);
            Assert.Single(files);
            
            string[] lines = File.ReadAllLines(files.First());

            Assert.Equal(3, lines.Length);

            // cleanup
            File.Delete(files.First());
        }

        [Fact]
        public void ErrorTest()
        {
            var files = Directory.GetFiles(_logDirectory);
            
            // setup
            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }

            Logger.Init();
            Logger.SetLogLevel(Logger.LogLevel.Error);

            // act
            Logger.Verbose("verbose");
            Logger.Debug("debug");
            Logger.Info("info");
            Logger.Error(new Exception("error"), "error");
            Logger.CloseAndFlush();

            // assert
            files = Directory.GetFiles(_logDirectory);
            Assert.Single(files);
            
            string[] lines = File.ReadAllLines(files.First());

            Assert.Equal(2, lines.Length);

            // cleanup
            File.Delete(files.First());
        }

        [Fact]
        public void OffTest()
        {
            var files = Directory.GetFiles(_logDirectory);
            
            // setup
            try
            {
                foreach (var file in files)
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }

            Logger.Init();
            Logger.SetLogLevel(Logger.LogLevel.Off);

            // act
            Logger.Verbose("verbose");
            Logger.Debug("debug");
            Logger.Info("info");
            Logger.Error(new Exception("error"), "error");
            Logger.CloseAndFlush();

            // assert
            files = Directory.GetFiles(_logDirectory);
            Assert.Empty(files);

            // cleanup
            try
            {
                File.Delete(files.First());
            }
            catch (Exception e)
            {
            }
        }
    }
}