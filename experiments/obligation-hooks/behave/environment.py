from probe import Probe


def before_scenario(context, scenario):
    context.probe = Probe("behave", f"{scenario.filename}:{scenario.line}:{scenario.name}")
    context.add_cleanup(context.probe.cleanup)


def after_scenario(context, scenario):
    context.probe.finish()


def before_step(context, step):
    context.probe.events.append(f"step:{step.name}")


def after_step(context, step):
    context.probe.events.append(f"step-status:{step.status.name}")
