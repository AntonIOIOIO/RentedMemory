# RentedMemory

RentedMemory is library that builds on ArrayPool to make it explicit that you are using a rented array and adds some functionality.

Includes:
-	RentedMemory is a readonly struct wrapper around ArrayPool use it kinda like dispose pattern.
-	RentedMemoryBuilder is a StringBuilder alternative with zero allocation (pooled) and works on any struct.
-	RentedObjects is a lightweight objectpool where you create the objects yourself.

[Nuget Package](https://www.nuget.org/packages/RentedMemory/)

# Getting Started

## RentedMemory
```csharp
// Rent a array of bytes with MinimumSize 24
var Rented = RentedMemory<byte>.Rent(MinimumSize: 24);

//Return it when you dont need it anymore
Rented.Return();
```
## RentedMemoryBuilder
```csharp
// Rent a array of char with MinimumSize 2
var RentedString = RentedMemoryBuilder<char>.Rent(MinimumSize: 2);

//RentedMemoryBuilder will grow if needed, still using ArrayPool
RentedString.Append("Hello World");
RentedString.Prepend("Hello World");
RentedString.Insert("Hello World");
RentedString.Replace("Hello World", "Hello");
RentedString.Remove("Hello");

//Write to the Span if you want full control, just remember to EnsureSize
RentedString.EnsureSize(FileLength)
int WrittenText = ReadTextFile(RentedString.RemainingSpan);
RentedString.AdvanceRemaining(WrittenText);

//Get the WrittenSpan or WrittenMemory when done and copy it somewhere, then return it
RentedString.WrittenSpan;
RentedString.WrittenMemory;
RentedString.Return();

//Or get the internal RentedMemory out when you are done building and ready to return the builder
RentedString.Return(out RentedMemory<char> RentedArray);
```
## RentedObjects
```csharp
// Create a new RentedObjects of objects
RentedObjects = new RentedObjects<object>(MaxPooledItemsSize: 16);

//Rents a object or returns null so you can create a new one
//object Object = RentedObjects.Rent() ?? new object();
object? Object = RentedObjects.Rent();

if(Object is null)
    Object = new object();

//Return it when you dont need it anymore
RentedObjects.Return(Object);
```