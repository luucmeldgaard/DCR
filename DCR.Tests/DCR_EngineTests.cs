using DCR.Core;
using DCR.Core.Components;

namespace DCR.Tests
{
    public class DCR_EngineTests
    {
        [Fact]
        public void ValidGraph_StartsWithoutError()
        {
            var engine = new DCR_Engine();
            var a = engine.AddActivity("A");
            var b = engine.AddActivity("B");
            engine.AddRule(a, b, RuleType.CONDITION);

            var exception = Record.Exception(() => engine.Start());
            Assert.Null(exception);
        }

        [Fact]
        public void RuleToUnknownActivity()
        {
            var engine = new DCR_Engine();
            var a = engine.AddActivity("A");
            var ghost = new Activity(Guid.NewGuid(), "Ghost", null, null);
            engine.AddRule(a, ghost, RuleType.RESPONSE);

            Assert.Throws<InvalidOperationException>(() => engine.Start());
        }
    }
}
