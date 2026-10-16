# Wisp Assembly Documentation
## Notes
* Sorry for the colors messed up in the examples.. They should look 'okay' still
* Only Push command uses 'f' and 's' to tell the type of the value. All other commands are uint so you dont need ints or floats. It will refuse to assemble with the wrong types
## Abreviations
* 'oStack' is the Operation Stack
* 'vStack' is the Variable Stack
* 'vHeap' and 'heap' is the Variable Heap
## Basic Commands
Essential commands to use

PUSH

Pushes a value into the Operation Stack (oStack)
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

STORE

Stores a value in oStack at an index
```asm
; example
store32 10

; All Possible store's (x == variable index in vStack)
store8 x    ; stores 8bit int/uint
store16 x   ; stores 16bit int/uint
store32 x   ; stores 32bit int/uint
store64 x   ; stores 64bit int/uint
storef32 x  ; stores 32bit float
storef64 x  ; stores 64bit float
```

LOAD

Pushes value from vStack into oStack, using an index you set from STORE
```asm

; example

loadu32 10 ; pushes uint32 value onto oStack from index 10 in vStack

; all versions
loadu8     ; loads uint8 value
loadu16    ; loads uint16 value
loadu32    ; loads uint32 value
loadu64    ; loads uint64 value
loads8     ; loads int8 value
loads16    ; loads int16 value
loads32    ; loads int32 value
loads64    ; loads int64 value
loadf32    ; loads float32 value
loadf64    ; loads float64 value
```

## Heap Control

HEAP-STORE

Stores value at an address in vHeap, value and address gotten from oStack
```asm
; Push Order for address/value
push VALUE
push ADDRESS

heap-store32

; all versions
heap-store8        ; stores 8bit int/uint in heap
heap-store16       ; stores 16bit int/uint in heap
heap-store32       ; stores 32bit int/uint in heap
heap-store64       ; stores 64bit int/uint in heap
heap-storef32      ; stores 32bit float in heap
heap-storef64      ; stores 64bit float in heap
```

HEAP-LOAD

Pushes value from address in vHeap

```asm
; example
push ADDRESS
heap-loadu32

; all versions
heap-loadu8     ; loads uint8 value in heap
heap-loadu16    ; loads uint16 value in heap
heap-loadu32    ; loads uint32 value in heap
heap-loadu64    ; loads uint64 value in heap
heap-loads8     ; loads int8 value in heap
heap-loads16    ; loads int16 value in heap
heap-loads32    ; loads int32 value in heap
heap-loads64    ; loads int64 value in heap
heap-loadf32    ; loads float32 value in heap
heap-loadf64    ; loads float64 value in heap

MALLOC

Allocates memory in the vHeap
```asm
; example
push memory_to_allocate (uint)
malloc
```

REF

Pushes full stack address of variable on vStack, gets variable index from argument
```asm
ref <vStack Variable Index>
; example
ref 10
```




More Assembly Documentation Comming Soon...
