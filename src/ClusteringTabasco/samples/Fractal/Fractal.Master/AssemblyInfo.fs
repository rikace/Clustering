namespace Fractal.Master.AssemblyInfo

open System.Reflection
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[<assembly: AssemblyTitle("Fractal.Master")>]
[<assembly: AssemblyDescription("")>]
[<assembly: AssemblyConfiguration("")>]
[<assembly: AssemblyCompany("")>]
[<assembly: AssemblyProduct("Fractal.Master")>]
[<assembly: AssemblyCopyright("Copyright ©  2016")>]
[<assembly: AssemblyTrademark("")>]
[<assembly: AssemblyCulture("")>]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[<assembly: ComVisible(false)>]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[<assembly: Guid("7a3afb37-f17d-4557-9f9c-150ed93b7890")>]

// Version information for an assembly consists of the following four values:
// 
//       Major Version
//       Minor Version 
//       Build Number
//       Revision
// 
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [<assembly: AssemblyVersion("1.0.*")>]
[<assembly: AssemblyVersion("1.0.0.0")>]
[<assembly: AssemblyFileVersion("1.0.0.0")>]

do
    ()


module Mandelbrot =

    open System.Drawing
    open System
    
    let set xp yp w h (width: int) (height: int) (maxr: double) (minr: double) (maxi: double) (mini: double) =
        let mutable img = new Bitmap(width = w, height = h)
        let mutable zx = 0.
        let mutable zy = 0.
        let mutable cx = 0.
        let mutable cy = 0.
        let mutable xjump = ((maxr - minr) / Convert.ToDouble(width))
        let mutable yjump = ((maxi - mini) / Convert.ToDouble(height))
        let mutable tempzx = 0.
        let mutable loopmax = 1000
        let mutable loopgo = 0
        for x = xp to xp + w - 1 do
            cx <- (xjump * (double x)) - Math.Abs(minr)
            for y = yp to yp + h - 1 do
                zx <- 0.
                zy <- 0.
                cy <- (yjump * (double y)) - Math.Abs(mini)
                loopgo <- 0
                while zx * zx + zy * zy <= 4. && loopgo < loopmax do
                    loopgo <- loopgo + 1
                    tempzx <- zx
                    zx <- (zx * zx) - (zy * zy) + cx
                    zy <- (2. * tempzx * zy) + cy
                    if loopgo <> loopmax then
                        img.SetPixel(x - xp, y - yp, Color.FromArgb(loopgo % 32 * 7, loopgo % 128 * 2, loopgo % 16 * 14))
                    else
                        img.SetPixel(x - xp, y - yp, Color.Black)
        img
