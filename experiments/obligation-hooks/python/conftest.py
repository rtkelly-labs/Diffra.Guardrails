from probe import Probe
import pytest


@pytest.fixture(autouse=True)
def lifecycle(request):
    if request.node.name.startswith("test_plain"):
        request.node.probe = Probe("pytest-plain", request.node.nodeid)
    yield
    if hasattr(request.node, "probe"):
        if request.node.name.startswith("test_plain"):
            request.node.probe.finish()
        request.node.probe.cleanup()


def pytest_bdd_before_scenario(request, feature, scenario):
    request.node.probe = Probe("pytest-bdd", request.node.nodeid)


def pytest_bdd_after_scenario(request, feature, scenario):
    request.node.probe.finish()


def pytest_bdd_before_step_call(request, feature, scenario, step, step_func, step_func_args):
    request.node.probe.events.append(f"binding:{step_func.__name__}")


def pytest_bdd_step_error(request, feature, scenario, step, step_func, step_func_args, exception):
    request.node.probe.events.append("step-error")
