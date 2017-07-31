using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    [TestClass]
    public class ResourceManagerDataSourceUnitTests : DataSourceUnitTestsBase
    {
        private const string FirstProjectName = "SomeProjectName";
        private const string FirstProjectId = "some-project-id";
        private const string SecondProjectName = "AProjectName";
        private const string SecondProjectId = "a-project-id";
        private const string DisabledProjectName = "DisabledProjectName";
        private const string DisabledProjectId = "disabled-project-id";

        private static readonly Project s_firstProject = new Project
        {
            Name = FirstProjectName,
            ProjectId = FirstProjectId,
            LifecycleState = ResourceManagerDataSource.LifecycleState.Active
        };

        private static readonly Project s_secondProject = new Project
        {
            Name = SecondProjectName,
            ProjectId = SecondProjectId,
            LifecycleState = ResourceManagerDataSource.LifecycleState.Active
        };

        private static readonly Project s_disabledProject = new Project
        {
            Name = DisabledProjectName,
            ProjectId = DisabledProjectId,
            LifecycleState = ResourceManagerDataSource.LifecycleState.DeleteRequested
        };

        [TestMethod]
        [ExpectedException(typeof(DataSourceException))]
        public async Task GetProjectsListAsync_Exception()
        {
            var responses = new ListProjectsResponse[0];
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), new object[0], responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            await dataSource.GetProjectsListAsync();
        }

        [TestMethod]
        public async Task GetProjectsListAsync_SinglePage()
        {
            var responses = new[]
            {
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_firstProject, s_disabledProject},
                    NextPageToken = null
                },
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_secondProject},
                    NextPageToken = null
                }
            };
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), new object[0], responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            IList<Project> projects = await dataSource.GetProjectsListAsync();

            Assert.AreEqual(2, projects.Count);
            Assert.AreEqual(s_firstProject, projects[0]);
            Assert.AreEqual(s_disabledProject, projects[1]);
        }

        [TestMethod]
        public async Task GetProjectsListAsync_MultiPage()
        {
            var responses = new[]
            {
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_firstProject, s_disabledProject},
                    NextPageToken = "2"
                },
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_secondProject},
                    NextPageToken = null
                }
            };
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), new object[0], responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            IList<Project> projects = await dataSource.GetProjectsListAsync();

            Assert.AreEqual(3, projects.Count);
            Assert.AreEqual(s_firstProject, projects[0]);
            Assert.AreEqual(s_disabledProject, projects[1]);
            Assert.AreEqual(s_secondProject, projects[2]);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException))]
        public async Task GetProject_Exception()
        {
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects,
                p => p.Get(It.IsAny<string>()),
                new[] { DummyString },
                new Project[0]);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            await dataSource.GetProjectAsync(FirstProjectId);
        }

        [TestMethod]
        public async Task GetProject_Success()
        {
            var responses = new[]
            {
                s_firstProject
            };
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects,
                p => p.Get(It.IsAny<string>()),
                new[] { DummyString },
                responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            Project project = await dataSource.GetProjectAsync(FirstProjectId);

            Assert.AreEqual(s_firstProject, project);
            Mock<ProjectsResource> projectsResource = Mock.Get(service.Projects);
            projectsResource.Verify(r => r.Get(FirstProjectId), Times.Once);
            projectsResource.Verify(r => r.Get(It.IsNotIn(FirstProjectId)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException))]
        public async Task GetSortedActiveProjectsAsync_Exception()
        {
            var responses = new ListProjectsResponse[0];
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), new object[0], responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            await dataSource.GetSortedActiveProjectsAsync();
        }

        [TestMethod]
        public async Task GetSortedActiveProjectsAsync_Success()
        {
            var responses = new[]
            {
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_firstProject, s_disabledProject},
                    NextPageToken = "2"
                },
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_secondProject},
                    NextPageToken = null
                }
            };
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), new object[0], responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            IList<Project> projects = await dataSource.GetSortedActiveProjectsAsync();

            Assert.AreEqual(2, projects.Count);
            Assert.AreEqual(s_secondProject, projects[0]);
            Assert.AreEqual(s_firstProject, projects[1]);
        }
    }
}
