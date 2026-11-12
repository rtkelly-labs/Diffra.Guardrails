from pytest_bdd import given, when, then, parsers, scenarios

scenarios("contract.feature")


@given(parsers.parse('the experiment case "{mode}"'))
def configure(request, mode):
    request.node.probe.configure(mode)


@when("the public reader returns rows through the recording transport")
def read(request):
    request.node.probe.read()


@then("the rows match the functional requirement")
def functional(request):
    request.node.probe.functional()


@then("the transport attempts meet the retry budget")
def policy(request):
    request.node.probe.policy()
