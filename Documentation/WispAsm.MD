# Wisp Assembly Documentation
## Abreviations
* 'OStack' is the Operation Stack
* 'VStack' is the Variable Stack
* 'VHeap' is the variable heap
## Basic Commands
```asm
; Pushes a value into the Operation Stack
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
