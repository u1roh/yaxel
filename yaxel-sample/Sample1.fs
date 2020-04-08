module Sample1

let helloWorld() = "hello world"

let hello (name: string) = sprintf "Hello, %s" name

let add (a: float) (b: float) = a + b

let mean (a: float) (b: float) = (add a b) / 2.0

let max (a: int) (b: int) =
    if a < b then b else a
