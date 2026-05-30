# MESH tool

A Windows tool for controlling [Sony MESH LED block](https://meshprj.com/) (MESH-100LE) via Bluetooth.

## Usage

### LED control

```
meshtool led --address <address> [options]
```

| Option | Description |
|:-|:-|
| `--address` / `-a` | Bluetooth address in hex (required) |
| `--color` / `-c` | RGB color in hex (default: FFFFFF) |
| `--time` / `-t` | Duration in ms, -1 = infinite (default: -1) |
| `--on` / `-n` | ON time in ms for blinking, -1 = infinite (default: -1) |
| `--off` / `-f` | OFF time in ms for blinking (default: 0) |
| `--pattern` / `-p` | Light pattern (default: 1) |

### Examples

```
meshtool led -a 112233445566
meshtool led -a 112233445566 -c FF0000
meshtool led -a 112233445566 -c 00FF00 -t 5000
meshtool led -a 112233445566 -c 0000FF -n 500 -f 500
```