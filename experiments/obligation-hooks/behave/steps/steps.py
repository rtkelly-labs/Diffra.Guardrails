from behave import given, when, then


@given('the experiment case "{mode}"')
def configure(context, mode):
    context.probe.configure(mode)


@when("the public reader returns rows through the recording transport")
def read(context):
    context.probe.read()


@then("the rows match the functional requirement")
def functional(context):
    context.probe.functional()


@then("the transport attempts meet the retry budget")
def policy(context):
    context.probe.policy()
