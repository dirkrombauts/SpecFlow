using System;
using BoDi;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using TechTalk.SpecFlow.Bindings;
using TechTalk.SpecFlow.Infrastructure;
using TechTalk.SpecFlow.Tracing;

namespace TechTalk.SpecFlow.RuntimeTests
{
    [TestFixture]
    public class ScenarioStepContextTests
    {
        [Test]
        public void CleanupStepContext_WhenNotInitialized_ShouldTraceWarning()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.CleanupStepContext();

            mockTracer.Verify(x => x.TraceWarning("The previous ScenarioStepContext was already disposed."));
        }

        /// <summary>
        /// Resolves the context manager and registers the provided test tracer.
        /// </summary>
        /// <param name="testTracer">The test tracer that will be registered.</param>
        /// <returns>An object that implements <see cref="IContextManager"/>.</returns>
        private IContextManager ResolveContextManager(ITestTracer testTracer)
        {
            var container = CreateObjectContainer(testTracer);
            var contextManager = container.Resolve<IContextManager>();
            return contextManager;
        }

        /// <summary>
        /// Creates an object container and registers the provided test tracer.
        /// </summary>
        /// <param name="testTracer">The test tracer that will be registered.</param>
        /// <returns>An object that implements <see cref="IObjectContainer"/>.</returns>
        private IObjectContainer CreateObjectContainer(ITestTracer testTracer)
        {
            IObjectContainer container;
            TestObjectFactories.CreateTestRunner(
                out container,
                objectContainer => objectContainer.RegisterInstanceAs<ITestTracer>(testTracer));
            return container;
        }

        [Test]
        public void InitializeStepContext_WhenInitializedTwice_ShouldNotTraceWarning()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize twice", null, string.Empty));

            mockTracer.Verify(x => x.TraceWarning(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void CleanupStepContext_WhenInitializedAsOftenAsCleanedUp_ShouldNotTraceWarning()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize twice", null, string.Empty));
            contextManager.CleanupStepContext();
            contextManager.CleanupStepContext();

            mockTracer.Verify(x => x.TraceWarning(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void CleanupStepContext_WhenCleanedUpMoreOftenThanInitialized_ShouldTraceWarning()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);


            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize twice", null, string.Empty));

            contextManager.CleanupStepContext();
            contextManager.CleanupStepContext();
            contextManager.CleanupStepContext();

            mockTracer.Verify(x => x.TraceWarning("The previous ScenarioStepContext was already disposed."), Times.Once());
        }

        public void StepContext_WhenInitializedOnce_ShouldReportStepInfo()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            var firstStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty);
            contextManager.InitializeStepContext(firstStepInfo);

            var actualStepInfo = contextManager.StepContext.StepInfo;

            Assert.AreEqual(firstStepInfo, actualStepInfo);
        }

        public void StepContext_WhenInitializedTwice_ShouldReportSecondStepInfo()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            var secondStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize twice", null, string.Empty);
            contextManager.InitializeStepContext(secondStepInfo);

            var actualStepInfo = contextManager.StepContext.StepInfo;

            Assert.AreEqual(secondStepInfo, actualStepInfo);
        }

        public void StepContext_WhenInitializedTwiceAndCleanedUpOnce_ShouldReportFirstStepInfo()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            var firstStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty);
            contextManager.InitializeStepContext(firstStepInfo);
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize twice", null, string.Empty));

            var actualStepInfo = contextManager.StepContext.StepInfo;

            Assert.AreEqual(firstStepInfo, actualStepInfo);
        }

        [Test]
        public void StepContext_WhenInitializedTwiceAndCleanedUpTwice_ShouldReportNoStepInfo()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize twice", null, string.Empty));
            contextManager.CleanupStepContext();
            contextManager.CleanupStepContext();

            Assert.AreEqual(null, contextManager.StepContext);
        }

        [Test]
        public void CurrentTopLevelStepDefinitionType_WhenInitialized_ShouldReportCorrectStepType()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));

            var actualCurrentTopLevelStepDefinitionType = contextManager.CurrentTopLevelStepDefinitionType;

            Assert.AreEqual(StepDefinitionType.Given, actualCurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void CurrentTopLevelStepDefinitionType_WhenInitializedTwice_ShouldReportFirstStepType()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.When, "I have called initialize twice", null, string.Empty));

            var actualCurrentTopLevelStepDefinitionType = contextManager.CurrentTopLevelStepDefinitionType;

            Assert.AreEqual(StepDefinitionType.Given, actualCurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void CurrentTopLevelStepDefinitionType_WhenInitializedTwiceAndCleanedUpOnce_ShouldReportFirstStepType()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.When, "I have called initialize twice", null, string.Empty));
            contextManager.CleanupStepContext(); // remove second

            var actualCurrentTopLevelStepDefinitionType = contextManager.CurrentTopLevelStepDefinitionType;

            Assert.AreEqual(StepDefinitionType.Given, actualCurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void CurrentTopLevelStepDefinitionType_WhenInitializedTwiceAndCleanedUpTwice_ShouldReportFirstStepType()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.When, "I have called initialize twice", null, string.Empty));
            contextManager.CleanupStepContext(); // remove second
            contextManager.CleanupStepContext(); // remove first

            var actualCurrentTopLevelStepDefinitionType = contextManager.CurrentTopLevelStepDefinitionType;

            Assert.AreEqual(StepDefinitionType.Given, actualCurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void CurrentTopLevelStepDefinitionType_AfterInitializationAndCleanupAndNewInitialization_ShouldReportSecondStepType()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.CleanupStepContext();
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.When, "I have called initialize twice", null, string.Empty));

            var actualCurrentTopLevelStepDefinitionType = contextManager.CurrentTopLevelStepDefinitionType;

            Assert.AreEqual(StepDefinitionType.When, actualCurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void CurrentTopLevelStepDefinitionType_AfterInitializationAndCleanupAndNewInitializationAndCleanup_ShouldReportSecondStepType()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty));
            contextManager.CleanupStepContext();
            contextManager.InitializeStepContext(new StepInfo(StepDefinitionType.When, "I have called initialize twice", null, string.Empty));
            contextManager.CleanupStepContext();

            var actualCurrentTopLevelStepDefinitionType = contextManager.CurrentTopLevelStepDefinitionType;

            Assert.AreEqual(StepDefinitionType.When, actualCurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void ShouldReportCorrectCurrentTopLevelStepIfWeHaveStepsMoreThan1Deep()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            var firstStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty);
            contextManager.InitializeStepContext(firstStepInfo);
            Assert.AreEqual(StepDefinitionType.Given, contextManager.CurrentTopLevelStepDefinitionType); // firstStepInfo

            contextManager.CleanupStepContext();
            var secondStepInfo = new StepInfo(StepDefinitionType.When, "I have called initialize twice", null, string.Empty);
            contextManager.InitializeStepContext(secondStepInfo);
            Assert.AreEqual(StepDefinitionType.When, contextManager.CurrentTopLevelStepDefinitionType); // secondStepInfo

            var thirdStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize a third time", null, string.Empty);
            contextManager.InitializeStepContext(thirdStepInfo); //Call sub step
            Assert.AreEqual(StepDefinitionType.When, contextManager.CurrentTopLevelStepDefinitionType); // secondStepInfo

            var fourthStepInfo = new StepInfo(StepDefinitionType.Then, "I have called initialize a forth time", null, string.Empty);
            contextManager.InitializeStepContext(fourthStepInfo); //call sub step of sub step
            contextManager.CleanupStepContext(); // return from sub step of sub step
            Assert.AreEqual(StepDefinitionType.When, contextManager.CurrentTopLevelStepDefinitionType); // secondStepInfo

            contextManager.CleanupStepContext(); // return from sub step
            Assert.AreEqual(StepDefinitionType.When, contextManager.CurrentTopLevelStepDefinitionType); // secondStepInfo

            contextManager.CleanupStepContext(); // finish 2nd step
            Assert.AreEqual(StepDefinitionType.When, contextManager.CurrentTopLevelStepDefinitionType); // secondStepInfo

            var fifthStepInfo = new StepInfo(StepDefinitionType.Then, "I have called initialize a fifth time", null, string.Empty);
            contextManager.InitializeStepContext(fifthStepInfo);
            Assert.AreEqual(StepDefinitionType.Then, contextManager.CurrentTopLevelStepDefinitionType); // fifthStepInfo
        }

        [Test]
        public void TopLevelStepShouldBeNullInitially()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            Assert.IsNull(contextManager.CurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void ScenarioStartShouldResetTopLevelStep()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            var firstStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty);
            contextManager.InitializeStepContext(firstStepInfo);
            // do not call CleanupStepContext to simulate inconsistent state

            contextManager.InitializeScenarioContext(new ScenarioInfo("my scenario"));

            Assert.IsNull(contextManager.CurrentTopLevelStepDefinitionType);
        }

        [Test]
        public void ShouldBeAbleToDisposeContextManagerAfterAnConsistentState()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            var firstStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty);
            contextManager.InitializeStepContext(firstStepInfo);
            // do not call CleanupStepContext to simulate inconsistent state

            ((IDisposable)contextManager).Dispose();
        }

        [Test]
        public void ShouldBeAbleToDisposeContextManagerAfterAnInconsistentState()
        {
            var mockTracer = new Mock<ITestTracer>();
            var contextManager = ResolveContextManager(mockTracer.Object);

            var firstStepInfo = new StepInfo(StepDefinitionType.Given, "I have called initialize once", null, string.Empty);
            contextManager.InitializeStepContext(firstStepInfo);
            contextManager.CleanupStepContext();

            ((IDisposable)contextManager).Dispose();
        }

        [Test]
        public void ShouldReportSetValuesCorrectly()
        {
            var table = new Table("header1","header2");
            const string multlineText = @" some
example
multiline
text";
            var stepInfo = new StepInfo(StepDefinitionType.Given, "Step text", table, multlineText);

            stepInfo.StepDefinitionType.Should().Be(StepDefinitionType.Given);
            stepInfo.Text.Should().Be("Step text");
            stepInfo.Table.Should().Be(table);
            stepInfo.MultilineText.Should().Be(multlineText);
        }
    }
}