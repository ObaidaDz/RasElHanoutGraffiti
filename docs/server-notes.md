# Server Notes

This is the setup I use for the public package:

- normal players get weekly sprays
- VIPs get more sprays and shorter cooldown
- admins can be unlimited
- spray wheel can be intercepted
- the `T` key can be bound to `css_spray`

If the bind does not apply for a player, they can run this once in console:

```text
bind t "css_spray"
```

The plugin is database-backed, so choices survive reconnects, map changes, and server restarts.
