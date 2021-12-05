using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace PracticingSix
{
    [Binding]
    public class EyeOfTheTigerSteps
    {
        public readonly ScenarioContext _scenarioContext;
        public EyeOfTheTigerSteps(ScenarioContext injectedContext)
        {
            _scenarioContext = injectedContext;
        }
        public EyeOfTheTiger eyeOfTheTiger { get { return new EyeOfTheTiger(_scenarioContext); } }

        [When(@"I need Eye of the Tiger every (.*) minutes")]
        public void WhenINeedEyeOfTheTigerEveryMinutes(double time)
        {
            eyeOfTheTiger.EyeMeEveryInterval(time);
        }

        [Then(@"I get what I need")]
        public void ThenIGetWhatINeed()
        {
            //ooh, yeah, that's the stuff
        }

    }
}
