module Mandelbrot 

    open System
    open System.Drawing
    open System.Threading
    open System.Threading.Tasks
    open System.Drawing
    
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

