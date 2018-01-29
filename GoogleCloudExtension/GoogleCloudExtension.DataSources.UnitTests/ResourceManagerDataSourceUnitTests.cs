// Copyright 2017 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
        private const string SomeProjectName = "SomeProjectName";
        private const string SomeProjectId = "some-project-id";
        private const string AProjectName = "AProjectName";
        private const string AProjectId = "a-project-id";
        private const string DisabledProjectName = "DisabledProjectName";
        private const string DisabledProjectId = "disabled-project-id";

        private static readonly Project s_someProject = new Project
        {
            Name = SomeProjectName,
            ProjectId = SomeProjectId,
            LifecycleState = ResourceManagerDataSource.LifecycleState.Active
        };

        private static readonly Project s_aProject = new Project
        {
            Name = AProjectName,
            ProjectId = AProjectId,
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
        public async Task GetProjectsListAsyncTestException()
        {
            // Empty response list triggers GoogleApiException.
            var responses = new ListProjectsResponse[0];
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            await dataSource.GetProjectsListAsync();
        }

        [TestMethod]
        public async Task GetProjectsListAsyncTestSinglePage()
        {
            var responses = new[]
            {
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_someProject, s_disabledProject},
                    NextPageToken = null
                },
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_aProject},
                    NextPageToken = null
                }
            };
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            IList<Project> projects = await dataSource.GetProjectsListAsync();

            Assert.AreEqual(1, projects.Count);
            Assert.AreEqual(s_someProject, projects[0]);
        }

        [TestMethod]
        public async Task GetProjectsListAsyncTestMultiPage()
        {
            var responses = new[]
            {
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_someProject, s_disabledProject},
                    NextPageToken = "2"
                },
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_aProject},
                    NextPageToken = null
                }
            };
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            IList<Project> projects = await dataSource.GetProjectsListAsync();

            Assert.AreEqual(2, projects.Count);
            Assert.AreEqual(s_someProject, projects[0]);
            Assert.AreEqual(s_aProject, projects[1]);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException))]
        public async Task GetProjectTestException()
        {
            // Empty response list triggers GoogleApiException.
            var responses = new Project[0];
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.Get(It.IsAny<string>()), responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            await dataSource.GetProjectAsync(SomeProjectId);
        }

        [TestMethod]
        public async Task GetProjectTestSuccess()
        {
            var responses = new[]
            {
                s_someProject
            };
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.Get(It.IsAny<string>()), responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            Project project = await dataSource.GetProjectAsync(SomeProjectId);

            Assert.AreEqual(s_someProject, project);
            Mock<ProjectsResource> projectsResource = Mock.Get(service.Projects);
            projectsResource.Verify(r => r.Get(SomeProjectId), Times.Once);
            projectsResource.Verify(r => r.Get(It.IsNotIn(SomeProjectId)), Times.Never);
        }

        [TestMethod]
        [ExpectedException(typeof(DataSourceException))]
        public async Task GetSortedActiveProjectsAsyncTestException()
        {
            // Empty response list triggers GoogleApiException.
            var responses = new ListProjectsResponse[0];
            CloudResourceManagerService service = GetMockedService(
                (CloudResourceManagerService s) => s.Projects, p => p.List(), responses);
            var dataSource = new ResourceManagerDataSource(null, init => service, null);

            await dataSource.GetProjectsListAsync();
        }
    }
}
