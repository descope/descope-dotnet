using Xunit;
using Descope.Mgmt.Models.Managementv1;
using Xunit.Abstractions;

namespace Descope.Test.Integration
{
    public class ProjectTests
    {
        private readonly IDescopeClient _descopeClient = IntegrationTestSetup.InitDescopeClient();
        private readonly ITestOutputHelper _output;

        public ProjectTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task Project_ExportImport()
        {
            try
            {
                // Export the project
                var exportRequest = new ExportSnapshotRequest();
                var originalExport = await _descopeClient.Mgmt.V1.Project.Export.PostAsync(exportRequest);
                Assert.NotNull(originalExport);

                // Import the project back using the extension method
                await _descopeClient.Mgmt.V1.Project.Import.PostWithExportedProjectAsync(originalExport!);

                // Export again and compare to original
                var reExportedProject = await _descopeClient.Mgmt.V1.Project.Export.PostAsync(exportRequest);
                Assert.NotNull(reExportedProject);

                // Compare the files - they should be identical since we just imported and re-exported
                Assert.NotNull(originalExport!.Files);
                Assert.NotNull(reExportedProject!.Files);

                _output.WriteLine("Comparing exported project files...");
                // Both should have the same file keys in AdditionalData
                if (originalExport.Files?.AdditionalData != null && reExportedProject.Files?.AdditionalData != null)
                {
                    Assert.Equal(originalExport.Files.AdditionalData.Count, reExportedProject.Files.AdditionalData.Count);

                    foreach (var key in originalExport.Files.AdditionalData.Keys)
                    {
                        _output.WriteLine($"Checking file key: {key}");
                        Assert.True(reExportedProject.Files.AdditionalData.ContainsKey(key),
                            $"Re-exported project should contain the same file key: {key}");
                    }
                }
            }
            finally
            {
                // No cleanup needed for export/import
            }
        }

        [Fact(Skip = "Flaky when run in CI")]
        public async Task Project_Rename()
        {
            var originalName = "Dotnet SDK Testing";
            try
            {
                // Rename to a new name
                var newName = Guid.NewGuid().ToString().Split("-").First();
                var renameRequest = new UpdateProjectNameRequest
                {
                    Name = newName
                };
                await _descopeClient.Mgmt.V1.Project.Update.Name.PostAsync(renameRequest);

                // Rename back to original name
                var restoreRequest = new UpdateProjectNameRequest
                {
                    Name = originalName
                };
                await _descopeClient.Mgmt.V1.Project.Update.Name.PostAsync(restoreRequest);
            }
            finally
            {
                // Make sure we restore the original name
                try
                {
                    var restoreRequest = new UpdateProjectNameRequest
                    {
                        Name = originalName
                    };
                    await _descopeClient.Mgmt.V1.Project.Update.Name.PostAsync(restoreRequest);
                }
                catch { }
            }
        }
    }
}
