## When to use


## And when not
In its current shape, minitor has serious limitations:
- **Security**: minitor *must NOT be exposed* to public access as its ease of use also makes it easy to abuse.
- **Persistence**: status information is only kept in memory and not saved to permanent storage, the reasoning being that status information should be refreshed by update sources at regular interval.
- **Alerting**: minitor does not send alerts, and does not even know about metrics, thresholds, schedules, historical data. It will show you issues if you ask but will not tell you by itself.

Simplicity is the key word,
