using System;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

using YamlDotNet.RepresentationModel;


namespace Amazon.Lambda.TestTool.Runtime
{
    /// <summary>
    /// This class handles getting the configuration information from aws-lambda-tools-defaults.json file 
    /// and possibly a CloudFormation template. YAML CloudFormation templates aren't supported yet.
    /// </summary>
    public static class LambdaTemplateFileParser
    {
        public static List<LambdaFunctionInfo> LoadLambdaFunctionsInfoFromTemplate(string templateFile)
        {
            var functionInfos = new List<LambdaFunctionInfo>();

            if (!string.IsNullOrEmpty(templateFile))
            {
                if (!File.Exists(templateFile))
                {
                    throw new FileNotFoundException($"Serverless template file {templateFile} not found");
                }

                functionInfos = LoadLambdaFunctionsInfoFromTemplate(JsonDocument.Parse(File.ReadAllText(templateFile).Trim()));
            }

            return functionInfos;
        }

        private static List<LambdaFunctionInfo> LoadLambdaFunctionsInfoFromTemplate(JsonDocument templateDocument)
        {
            var functionInfos = new List<LambdaFunctionInfo>();

            JsonElement resourcesNode;
            if (!templateDocument.RootElement.TryGetProperty("Resources", out resourcesNode))
                return functionInfos;

            foreach (var resourceProperty in resourcesNode.EnumerateObject())
            {
                var resource = resourceProperty.Value;

                JsonElement typeProperty;
                if (!resource.TryGetProperty("Type", out typeProperty))
                    continue;

                var type = typeProperty.GetString();

                JsonElement propertiesProperty;
                if (!resource.TryGetProperty("Properties", out propertiesProperty))
                    continue;


                if (!string.Equals("AWS::Serverless::Function", type, StringComparison.Ordinal) &&
                    !string.Equals("AWS::Lambda::Function", type, StringComparison.Ordinal))
                {
                    continue;
                }

                string handler = null;
                if (propertiesProperty.TryGetProperty("Handler", out var handlerProperty))
                {
                    handler = handlerProperty.GetString();
                }
                else if(propertiesProperty.TryGetProperty("ImageConfig", out var imageConfigProperty) &&
                        imageConfigProperty.TryGetProperty("Command", out var imageCommandProperty))
                {
                    if(imageCommandProperty.GetArrayLength() > 0)
                    {
                        // Grab the first element assuming that is the function handler.
                        var en = imageCommandProperty.EnumerateArray();
                        en.MoveNext();
                        handler = en.Current.GetString();
                    }
                }

                if (!string.IsNullOrEmpty(handler))
                {
                    var functionInfo = new LambdaFunctionInfo
                    {
                        Name = resourceProperty.Name,
                        Handler = handler
                    };

                    functionInfos.Add(functionInfo);
                }
            }

            return functionInfos;
        }
    }
}