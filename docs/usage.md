# When to use

Although it is simple, or even simplistic, Minitor can be another swiss knife on your toolbelt.

It is not built to gather detailed information about everything but is very capable for easily building synthetic views
of multiple systems and operations.

## Dashboard

Minitor makes an easy to build heartbeat dashboard, using `Validity` and `Expiration` parameters. `Heartbeat` can also be added to change expiration behavior.

With minimal effort, it can reveal degraded or error status in many component and many system.

## Long running operations

Long running and distributed operations benefit from a centralized status view.

Inserting simple commands in the chain of operations provides valuable feedback on progress and success.

## Local status

As it does not require setup nor configuration, Minitor can be useful as an ad-hoc visualization tool during development, migrations, environments staging...

It provides a useful synthetic view which nicely complements detailed activity logs.

# And when not

In its current shape minitor has serious limitations, please be aware of some below.

## Security

Minitor __must NOT be exposed__ to public access as its ease of use also makes it easy to abuse.

It does not know about authentication, authorization, requests inspection, rate limiting and other such important security-related things.

It is built for use in trusted environment, by trusted users.

## Persistence

Status information is only kept in memory and not saved to permanent storage.

The reasoning is that status information should be refreshed by update sources at regular interval, and its retention does not have much value.

This means there is also not status history.

Persistence might be added in the future.

## Alerting

Minitor does not send alerts, and does not even know about metrics, thresholds, schedules, historical data and such.

It will show you the state of your environment and may help uncover issues, but it will not tell you anything by itself.

Simplicity is the key word.
