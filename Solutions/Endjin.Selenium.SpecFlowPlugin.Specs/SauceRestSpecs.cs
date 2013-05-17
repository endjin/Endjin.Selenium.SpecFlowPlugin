using Machine.Specifications;

namespace Endjin.Selenium.SpecFlowPlugin.Specs
{
    [Subject(typeof(SauceRest))]
    public class when_sauce_rest_is_asked_to_update_the_job_status_to_passed
    {
        private static SauceRest sut;

        private static string jobId;

        Establish context = () =>
        {
            sut = new SauceRest("gnewman", "78c79ae6-a8ae-489e-88b0-328e96d932e0", "http://saucelabs.com/rest");
            jobId = "1a56f02c4bdc4ed386fbc8db7ee16d07";
        };

        Because of = () => sut.SetJobPassed(jobId);

        It should_update_the_job_status_passed = () => { };
    }
}