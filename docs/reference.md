# URL Parameters

For the general URL format, see the [quick start](start.md).

All parameter names and status values can be abbreviated.

## Monitor

This is used both as a display name and as a unique key for each monitor.
It is required and has no default value.

Obviously, http://localhost/set/Production?monitor=SERVER and http://localhost/set/Test?monitor=SERVER do refer to different monitors.

## Status

The status of every monitor can be one of:

| Value | Color | Meaning |
| --- | --- | --- |
| Normal | White | This is the default value and it has no other meaning than showing that everything is right and life is beautiful. |
| Completed | Green | `Completed` is intended to be used to signal the successful end of a lengthy operation. As such, it is NOT subject to `Validity` timeout.<br />To show success for other kind of operations, use `Normal`. |
| Unknown | Blue | Not commonly set manually, it can be activated automatically using `Validity` parameter. |
| Warning | Yellow | Indicates a minor problem. |
| Error | Red | Shows a more serious problem. |
| Dead | Black | Also not commonly set manually, but can be set automatically using `Expiration` parameter. |

This table is sorted by priority order, for instance any path with at least a monitor has `Error` status will show as error unless another monitor has `Dead` status.

In other words, `Dead` takes priority over `Error`, which takes priority over `Warning` and so on...

## Validity

This parameters says for how long a status update is valid.

After that time has ellapsed, the monitor automatically turns to `Unknown` status.

It is an integer number followed by a unit letter:
- `s` for seconds
- `m` for minutes (default)
- `h` for hours
- `d` for days
- `w` for weeks

For instance:
```
http://localhost/set/Some/Path?monitor=SRV&validity=30m
```

## Expiration

Similar to `Validity`, but when the specified time has ellapsed the monitor is removed.

Or, if the given number is negative, the monitor turns to `Dead` status:

```
http://localhost/set/Some/Path?monitor=SRV&expiration=-30m
```

To explicitely delete a monitor, use a zero expiration:

```
http://localhost/set/Some/Path?monitor=SRV&expiration=0
```

`Expiration` and `Validity` can and often should be combined.


## Text

`text` is an optional descriptive text, if specified it will be shown next to each monitor name.

If omitted, any previous `text` is removed.

# Command line

For now Minitor only accepts one parameter on command line (`server`), which is required to run the service.
