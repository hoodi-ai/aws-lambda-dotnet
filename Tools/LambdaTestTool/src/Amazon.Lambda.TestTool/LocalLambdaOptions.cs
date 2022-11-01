using Amazon.Lambda.TestTool.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Amazon.Lambda.TestTool
{
    public class LocalLambdaOptions
    {
        public string Host { get; set; }

        public int? Port { get; set; }

        public string DefaultAWSProfile { get; set; }

        public string DefaultAWSRegion { get; set; }

        public string DefaultTemplateFile { get; set; }

        public IList<string> TemplateFiles { get; set; }

        public ILocalLambdaRuntime LambdaRuntime { get; set; }

        public LambdaFunction LoadLambdaFuntion(string templateFile, string functionHandler)
        {
            var fullTemplateFilePath = this.TemplateFiles.FirstOrDefault(x =>
                string.Equals(templateFile, x, StringComparison.OrdinalIgnoreCase) || string.Equals(templateFile, Path.GetFileName(x), StringComparison.OrdinalIgnoreCase));
            if (fullTemplateFilePath == null)
            {
                throw new Exception($"Failed to find template {templateFile}");
            }

            var functionsInfo = LambdaTemplateFileParser.LoadLambdaFunctionsInfoFromTemplate(fullTemplateFilePath);

            var functionInfo = functionsInfo.FirstOrDefault(x =>
                string.Equals(functionHandler, x.Handler, StringComparison.OrdinalIgnoreCase));
            if (functionInfo == null)
            {
                throw new Exception($"Failed to find function {functionHandler}");
            }

            var function = this.LambdaRuntime.LoadLambdaFunction(functionInfo);
            return function;
        }

        /// <summary>
        /// The directory to store in local settings for a Lambda project for example saved Lambda requests.
        /// </summary>
        public string GetPreferenceDirectory(bool createIfNotExist)
        {
            var currentDirectory = this.LambdaRuntime.LambdaAssemblyDirectory;
            while (currentDirectory != null && !Utils.IsProjectDirectory(currentDirectory))
            {
                currentDirectory = Directory.GetParent(currentDirectory).FullName;
            }

            if (currentDirectory == null)
                currentDirectory = this.LambdaRuntime.LambdaAssemblyDirectory;

            var preferenceDirectory = Path.Combine(currentDirectory, ".lambda-test-tool");
            if (createIfNotExist && !Directory.Exists(preferenceDirectory))
            {
                Directory.CreateDirectory(preferenceDirectory);
            }

            return preferenceDirectory;
        }
    }
}
