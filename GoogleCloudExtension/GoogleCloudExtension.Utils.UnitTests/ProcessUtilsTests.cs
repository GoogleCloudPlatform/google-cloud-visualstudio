using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace GoogleCloudExtension.Utils.UnitTests
{
    [SuppressMessage("ReSharper", "UnassignedField.Global")]
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class JsonDataClass
    {
        public string Var;
    }

    [TestClass]
    [DeploymentItem(EchoAppName)]
    public class ProcessUtilsTests
    {
        private const string ProcessOutput = "ProcessOutput";
        private const string StdOutArgs = "-out " + ProcessOutput;
        private const string StdErrArgs = "-err " + ProcessOutput;
        private const string ExpArgs = "-exp " + ProcessOutput;
        private const string JsonArgs = "-out \"{\"" + nameof(JsonDataClass.Var) + "\":'" + ProcessOutput + "'}\"";
        private const string EchoAppName = "EchoApp.exe";

        [TestMethod]
        [ExpectedException(typeof(Win32Exception))]
        public async Task GetCommandOutputAsync_TargetInvalid()
        {
            await ProcessUtils.GetCommandOutputAsync("BadCommand.exe", StdOutArgs);
        }

        [TestMethod]
        public async Task GetCommandOutputAsync_StandardOutput()
        {
            ProcessOutput output = await ProcessUtils.GetCommandOutputAsync(EchoAppName, StdOutArgs);

            Assert.IsTrue(output.Succeeded);
            Assert.AreEqual(ProcessOutput, output.StandardOutput);
            Assert.AreEqual(string.Empty, output.StandardError);
        }

        [TestMethod]
        public async Task GetCommandOutputAsync_StdErr()
        {
            ProcessOutput output = await ProcessUtils.GetCommandOutputAsync(EchoAppName, StdErrArgs);

            Assert.IsTrue(output.Succeeded);
            Assert.AreEqual(string.Empty, output.StandardOutput);
            Assert.AreEqual(ProcessOutput, output.StandardError);
        }

        [TestMethod]
        public async Task GetCommandOutputAsync_Exp()
        {
            ProcessOutput output = await ProcessUtils.GetCommandOutputAsync(EchoAppName, ExpArgs);

            Assert.IsFalse(output.Succeeded);
            Assert.AreEqual(ProcessOutput, output.StandardOutput);
            Assert.AreEqual(string.Empty, output.StandardError);
        }

        [TestMethod]
        [ExpectedException(typeof(Win32Exception))]
        public async Task GetJsonOutputAsync_InvalidTarget()
        {
            await ProcessUtils.GetJsonOutputAsync<string>("BadTarget.exe", StdOutArgs);
        }

        [TestMethod]
        [ExpectedException(typeof(JsonOutputException))]
        public async Task GetJsonOutputAsync_ProcessError()
        {
            await ProcessUtils.GetJsonOutputAsync<string>(EchoAppName, ExpArgs);
        }

        [TestMethod]
        [ExpectedException(typeof(JsonOutputException))]
        public async Task GetJsonOutputAsync_InvalidJson()
        {
            await ProcessUtils.GetJsonOutputAsync<string>(EchoAppName, StdOutArgs);
        }

        [TestMethod]
        public async Task GetJsonOutputAsync_Success()
        {
            JsonDataClass output = await ProcessUtils.GetJsonOutputAsync<JsonDataClass>(EchoAppName, JsonArgs);

            Assert.IsNotNull(output);
            Assert.IsNotNull(output.Var);
            Assert.AreEqual(ProcessOutput, output.Var);
        }
    }
}
