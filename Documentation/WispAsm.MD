# Wisp Assembly Documentation
## Notes
* Sorry for the colors messed up in the examples.. They should look 'okay' still
* Only Push command uses 'f' and 's' to tell the type of the value. All other commands are uint so you dont need ints or floats. It will refuse to assemble with the wrong types
## Abreviations
* 'oStack' is the Operation Stack
* 'vStack' is the Variable Stack
* 'vHeap' is the variable heap
## Basic Commands
Essential commands to use

PUSH

Pushes a value into the Operation Stack
```asm

push <number>

; examples
push 10    ; pushes 10 as uint64
push 10s   ; pushes 10 as int64
push 10f   ; pushes 10 as float64

push 10.52f
push -9123s

; 's' for signed int value
; 'f' for floating point value
```


## Stack Control
ENTER

Reserves space in the vStack for variables in the method/label
```asm
enter <value>

; examples
enter 10
enter 16
enter 90231

```
