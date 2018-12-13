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

using Google;
using Google.Apis.CloudResourceManager.v1;
using Google.Apis.CloudResourceManager.v1.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoogleCloudExtension.DataSources.UnitTests
{
    [TestClass]
    public class ResourceManagerDataSourceUnitTests
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

        private Mock<CloudResourceManagerService> _serviceMock;
        private ResourceManagerDataSource _objectUnderTest;
        private Mock<ProjectsResource> _projectsResourceMock;
        private static readonly GoogleApiException s_googleApiException = new GoogleApiException(nameof(CloudResourceManagerService), "MockExceptionMessage");

        [TestInitialize]
        public void BeforeEach()
        {
            _serviceMock = new Mock<CloudResourceManagerService>();
            _projectsResourceMock = _serviceMock.Resource(s => s.Projects);
            _objectUnderTest = new ResourceManagerDataSource(null, init => _serviceMock.Object, null);
        }

        [TestMethod]
        public async Task GetProjectsListAsyncTestException()
        {
            _projectsResourceMock.Request(p => p.List()).Response<ListProjectsResponse>().Throws(s_googleApiException);

            await Assert.ThrowsExceptionAsync<DataSourceException>(() => _objectUnderTest.GetProjectsListAsync());
        }

        [TestMethod]
        public async Task ProjectsListTask()
        {
            ListProjectsResponse[] responses = {
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
            _projectsResourceMock.Request(p => p.List()).ResponseReturns(responses);
            _objectUnderTest = new ResourceManagerDataSource(null, init => _serviceMock.Object, null);

            IList<Project> projects = await _objectUnderTest.ProjectsListTask;

            Assert.AreEqual(1, projects.Count);
            Assert.AreEqual(s_someProject, projects[0]);
        }

        [TestMethod]
        public async Task RefreshProjects_RestartsProjectsListTask()
        {
            ListProjectsResponse[] responses = {
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_aProject},
                    NextPageToken = null
                },
                new ListProjectsResponse
                {
                    Projects = new List<Project> {s_someProject, s_disabledProject},
                    NextPageToken = null
                }
            };
            _projectsResourceMock.Request(p => p.List()).ResponseReturns(responses);

            _objectUnderTest.RefreshProjects();
            IList<Project> projects = await _objectUnderTest.ProjectsListTask;

            Assert.AreEqual(1, projects.Count);
            Assert.AreEqual(s_aProject, projects[0]);
        }

        [TestMethod]
        public async Task GetProjectsListAsyncTestSinglePage()
        {
            ListProjectsResponse[] responses = {
                new ListProjectsResponse(),
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
            _projectsResourceMock.Request(p => p.List()).ResponseReturns(responses);
            _objectUnderTest = new ResourceManagerDataSource(null, init => _serviceMock.Object, null);

            IList<Project> projects = await _objectUnderTest.GetProjectsListAsync();

            Assert.AreEqual(1, projects.Count);
            Assert.AreEqual(s_someProject, projects[0]);
        }

        [TestMethod]
        public async Task GetProjectsListAsyncTestMultiPage()
        {
            ListProjectsResponse[] responses = {
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
            _projectsResourceMock.Request(p => p.List()).ResponseReturns(responses);

            IList<Project> projects = await _objectUnderTest.GetProjectsListAsync();

            Assert.AreEqual(2, projects.Count);
            Assert.AreEqual(s_someProject, projects[0]);
            Assert.AreEqual(s_aProject, projects[1]);
        }

        [TestMethod]
        public async Task GetProjectTestException()
        {
            _projectsResourceMock.Request(p => p.Get(It.IsAny<string>()))
                .Response<Project>()
                .Throws(s_googleApiException);

            await Assert.ThrowsExceptionAsync<DataSourceException>(
                () => _objectUnderTest.GetProjectAsync(SomeProjectId));
        }

        [TestMethod]
        public async Task GetProjectTestSuccess()
        {
            _projectsResourceMock.Request(p => p.Get(It.IsAny<string>())).ResponseReturns(s_someProject);

            Project project = await _objectUnderTest.GetProjectAsync(SomeProjectId);

            Assert.AreEqual(s_someProject, project);
            _projectsResourceMock.Verify(r => r.Get(SomeProjectId), Times.Once);
            _projectsResourceMock.Verify(r => r.Get(It.IsNotIn(SomeProjectId)), Times.Never);
        }
    }
}
