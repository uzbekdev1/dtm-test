using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using DTM.Test.OMR.Models;
using System.Configuration;

namespace DTM.Test.OMR.Helpers
{
    public sealed class ParserHelper : IDisposable
    {
        private const string TEST_FILE_NAME = "Census.mxml";
        private readonly string _engineExcutablePath = Path.Combine(ConfigurationManager.AppSettings["EngineExcutableDirectory"], "OmrConsole.exe");
        private readonly string _outputFilePath = Path.Combine(ConfigurationManager.AppSettings["EngineOutputDirectory"], Path.GetRandomFileName());
        private readonly string _templateDirectory = Path.Combine(ConfigurationManager.AppSettings["EngineTemplateDirectory"], TEST_FILE_NAME);
        private readonly string _inputFilePath;
        private string _outputMessage;

        public ParserHelper(string inputFileName)
        {
            _inputFilePath = Path.Combine(ConfigurationManager.AppSettings["EngineUploadDirectory"], inputFileName);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        public void Analizy()
        {
            if (!File.Exists(_templateDirectory))
            {
                _outputMessage = "DTM test template is not found!";
                LogHelper.Logger.Error(_outputMessage);

                return;
            }

            var engineParameters = string.Format("-s \"{0}\" -t \"{1}\" -o \"{2}\"", _inputFilePath, _templateDirectory, _outputFilePath);

            _outputMessage = CommandLineHelper.Run(_engineExcutablePath, engineParameters);

        }

        public string OutputMessage
        {
            get { return _outputMessage; }
        }

        public TestResultModel GetResult()
        {
            if (!String.IsNullOrWhiteSpace(_outputMessage))
                return null;

            var xml = File.ReadAllText(_outputFilePath, Encoding.UTF8);
            var model = XMLHelper.Deserialize<TestResultModel>(xml);

            return model;
        }
    }
}