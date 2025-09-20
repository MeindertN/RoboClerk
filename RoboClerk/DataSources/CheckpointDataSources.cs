using RoboClerk.Configuration;
using RoboClerk.Items;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Text.Json;

namespace RoboClerk
{
    public class CheckpointDataSources : DataSourcesBase
    {
        private CheckpointDataStorage dataStorage = new CheckpointDataStorage();
        private IDataSources pluginDatasource = null;

        public CheckpointDataSources(IConfiguration configuration, IPluginLoader pluginLoader, IFileSystem fileSystem, string checkpointFile)
            : base(configuration, fileSystem)
        {
            logger.Info($"RoboClerk is using the following checkpoint file in the template directory to read its input data: {checkpointFile}");
            pluginDatasource = new PluginDataSources(configuration, pluginLoader, fileSystem);
            SetFileSource(checkpointFile);
            ChangeUpdatedItems();
        }

        public void SetFileSource(string fileName)
        {
            string fullFilePath = fileSystem.Path.Join(configuration.TemplateDir, fileName);
            if (!fileSystem.File.Exists(fullFilePath))
            {
                throw new Exception($"Could not find checkpoint file \"{fullFilePath}\". Unable to continue.");
            }
            dataStorage = JsonSerializer.Deserialize<CheckpointDataStorage>(GetFileStreamFromTemplateDir(fileName));
        }

        private void ChangeUpdatedItems()
        {
            var checkpointConfig = configuration.CheckpointConfig;
            foreach (var riskID in checkpointConfig.UpdatedRiskIDs)
            {
                var sourceRiskItem = pluginDatasource.GetRisk(riskID);
                if (sourceRiskItem != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateRisk(sourceRiskItem);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveRisk(riskID);
                }
            }
            foreach (var systemRequirementID in checkpointConfig.UpdatedSystemRequirementIDs)
            {
                var systemRequirement = pluginDatasource.GetSystemRequirement(systemRequirementID);
                if (systemRequirement != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateSystemRequirement(systemRequirement);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveSystemRequirement(systemRequirementID);
                }
            }
            foreach (var softwareRequirementID in checkpointConfig.UpdatedSoftwareRequirementIDs)
            {
                var softwareRequirement = pluginDatasource.GetSoftwareRequirement(softwareRequirementID);
                if (softwareRequirement != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateSoftwareRequirement(softwareRequirement);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveSoftwareRequirement(softwareRequirementID);
                }
            }
            foreach (var documentationRequirementID in checkpointConfig.UpdatedDocumentationRequirementIDs)
            {
                var documentationRequirement = pluginDatasource.GetDocumentationRequirement(documentationRequirementID);
                if (documentationRequirement != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateDocumentationRequirement(documentationRequirement);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveDocumentationRequirement(documentationRequirementID);
                }
            }
            foreach (var docContentID in checkpointConfig.UpdatedDocContentIDs)
            {
                var docContent = pluginDatasource.GetDocContent(docContentID);
                if (docContent != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateDocContent(docContent);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveDocContent(docContentID);
                }
            }
            foreach (var softwareSystemTestID in checkpointConfig.UpdatedSoftwareSystemTestIDs)
            {
                var softwareSystemTest = pluginDatasource.GetSoftwareSystemTest(softwareSystemTestID);
                if (softwareSystemTest != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateSoftwareSystemTest(softwareSystemTest);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveSoftwareSystemTest(softwareSystemTestID);
                }
            }
            foreach (var unitTestID in checkpointConfig.UpdatedUnitTestIDs)
            {
                var unitTest = pluginDatasource.GetUnitTest(unitTestID);
                if (unitTest != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateUnitTest(unitTest);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveUnitTest(unitTestID);
                }
            }
            foreach (var anomalyID in checkpointConfig.UpdatedAnomalyIDs)
            {
                var anomaly = pluginDatasource.GetAnomaly(anomalyID);
                if (anomaly != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateAnomaly(anomaly);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveAnomaly(anomalyID);
                }
            }
            foreach (var soupID in checkpointConfig.UpdatedSOUPIDs)
            {
                var soup = pluginDatasource.GetSOUP(soupID);
                if (soup != null)
                {
                    //item still exists, update it
                    dataStorage.UpdateSOUP(soup);
                }
                else
                {
                    //item no longer exists, should remove from checkpoint data
                    dataStorage.RemoveSOUP(soupID);
                }
            }
        }

        public override List<AnomalyItem> GetAllAnomalies()
        {
            return dataStorage.Anomalies;
        }

        public override List<EliminatedAnomalyItem> GetAllEliminatedAnomalies()
        {
            return dataStorage.EliminatedAnomalies;
        }

        public override List<ExternalDependency> GetAllExternalDependencies()
        {
            return pluginDatasource.GetAllExternalDependencies();
        }

        public override List<TestResult> GetAllTestResults()
        {
            return pluginDatasource.GetAllTestResults(); 
        }

        public override List<RiskItem> GetAllRisks()
        {
            return dataStorage.Risks;
        }

        public override List<EliminatedRiskItem> GetAllEliminatedRisks()
        {
            return dataStorage.EliminatedRisks;
        }

        public override List<RequirementItem> GetAllSoftwareRequirements()
        {
            return dataStorage.SoftwareRequirements;
        }

        public override List<EliminatedRequirementItem> GetAllEliminatedSoftwareRequirements()
        {
            return dataStorage.EliminatedSoftwareRequirements;
        }

        public override List<SoftwareSystemTestItem> GetAllSoftwareSystemTests()
        {
            return dataStorage.SoftwareSystemTests;
        }

        public override List<EliminatedSoftwareSystemTestItem> GetAllEliminatedSoftwareSystemTests()
        {
            return dataStorage.EliminatedSoftwareSystemTests;
        }

        public override List<UnitTestItem> GetAllUnitTests()
        {
            return dataStorage.UnitTests;
        }

        public override List<SOUPItem> GetAllSOUP()
        {
            return dataStorage.SOUPs;
        }

        public override List<EliminatedSOUPItem> GetAllEliminatedSOUP()
        {
            return dataStorage.EliminatedSOUPs;
        }

        public override List<RequirementItem> GetAllSystemRequirements()
        {
            return dataStorage.SystemRequirements;
        }

        public override List<EliminatedRequirementItem> GetAllEliminatedSystemRequirements()
        {
            return dataStorage.EliminatedSystemRequirements;
        }

        public override List<RequirementItem> GetAllDocumentationRequirements()
        {
            return dataStorage.DocumentationRequirements;
        }

        public override List<EliminatedRequirementItem> GetAllEliminatedDocumentationRequirements()
        {
            return dataStorage.EliminatedDocumentationRequirements;
        }

        public override List<DocContentItem> GetAllDocContents()
        {
            return dataStorage.DocContents;
        }

        public override List<EliminatedDocContentItem> GetAllEliminatedDocContents()
        {
            return dataStorage.EliminatedDocContents;
        }
    }
}
