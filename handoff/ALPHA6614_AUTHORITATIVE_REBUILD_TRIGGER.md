# Alpha 6.6.14 authoritative rebuild trigger

This checkpoint intentionally changes no gameplay source. It triggers CI again after the Alpha 6.6.14 generated source was materialized at `a6d44942d5296943528e2ff8aa3a77184d7c7932`.

The authoritative run must build from the already-materialized source and report `No materialized source diff.` before the test package is handed off.
