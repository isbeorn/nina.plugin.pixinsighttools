function abe() {

    let stackFile = jsArguments[0];
    let degree = jsArguments[1];
    let outputFile = jsArguments[2];
    let outputDir = jsArguments[3];

    console.writeln("stackFile = " + stackFile);
    console.writeln("degree = " + degree);
    console.writeln("outputFile = " + outputFile);
    console.writeln("outputDir = " + outputDir);

    let stackFileInfo = new FileInfo(stackFile);

    if (stackFileInfo.isFile) {
        let stackWindow = ImageWindow.open(stackFile);

        if (degree > 0) {
            let P = new AutomaticBackgroundExtractor;
            P.tolerance = 1.000;
            P.deviation = 0.800;
            P.unbalance = 1.800;
            P.minBoxFraction = 0.050;
            P.maxBackground = 1.0000;
            P.minBackground = 0.0000;
            P.useBrightnessLimits = false;
            P.polyDegree = 4;
            P.boxSize = 5;
            P.boxSeparation = 5;
            P.modelImageSampleFormat = AutomaticBackgroundExtractor.prototype.f32;
            P.abeDownsample = 2.00;
            P.writeSampleBoxes = false;
            P.justTrySamples = false;
            P.targetCorrection = AutomaticBackgroundExtractor.prototype.Subtract;
            P.normalize = false;
            P.discardModel = true;
            P.replaceTarget = true;
            P.correctedImageId = "";
            P.correctedImageSampleFormat = AutomaticBackgroundExtractor.prototype.SameAsTarget;
            P.verboseCoefficients = false;
            P.compareModel = false;
            P.compareFactor = 10.00;
            P.noGUIMessages = true;

            P.executeOn(stackWindow[0].mainView);
        }

        new AutoStretch().HardApply(stackWindow[0].mainView);

        stackWindow[0].saveAs(outputFile, false, false, false, false);
        stackWindow[0].forceClose();

        console.writeln("Writing textfile: " + outputDir + "/done_abe.txt");
        File.writeTextFile(outputDir + "/done_abe.txt", "done");
    } else {
        console.writeln("Input file does not seem to exist");

        console.writeln("Writing textfile: " + outputDir + "/error_abe.txt");
        File.writeTextFile(outputDir + "/error_abe.txt", "error");
    }
}

abe();

function AutoStretch() {
    // Default STF Parameters
    let SHADOWS_CLIP = -1.25; // Shadows clipping point measured in sigma units from the main histogram peak.
    let TARGET_BKG = 0.25; // Target background in the [0,1] range.

    /*
     * Find a midtones balance value that transforms v1 into v0 through a midtones
     * transfer function (MTF), within the specified tolerance eps.
     */
    this.findMidtonesBalance = function (v0, v1, eps) {
        if (v1 <= 0)
            return 0;

        if (v1 >= 1)
            return 1;

        v0 = Math.range(v0, 0.0, 1.0);

        if (eps)
            eps = Math.max(1.0e-15, eps);
        else
            eps = 5.0e-05;

        let m0, m1;
        if (v1 < v0) {
            m0 = 0;
            m1 = 0.5;
        }
        else {
            m0 = 0.5;
            m1 = 1;
        }

        for (; ;) {
            let m = (m0 + m1) / 2;
            let v = Math.mtf(m, v1);

            if (Math.abs(v - v0) < eps)
                return m;

            if (v < v0)
                m1 = m;
            else
                m0 = m;
        }
    }

    this.CalculateStretch = function (view, verbose, shadowsClipping, targetBackground) {
        if (shadowsClipping == undefined)
            shadowsClipping = SHADOWS_CLIP;
        if (targetBackground == undefined)
            targetBackground = TARGET_BKG;
        if (verbose == undefined)
            verbose = false;

        view.image.resetSelections();

        // Noninverted image

        let c0 = 0;
        let m = 0;
        view.image.selectedChannel = 0;
        let median = view.image.median();
        let avgDev = view.image.avgDev();
        c0 += median + shadowsClipping * avgDev;
        m += median;
        view.image.resetSelections();
        c0 = Math.range(c0, 0.0, 1.0);
        m = this.findMidtonesBalance(targetBackground, m - c0);

        return { m: m, c0: c0, c1: 1 };
    }
    /*
     * STF Auto Stretch routine
     */
    this.Apply = function (view, verbose, shadowsClipping, targetBackground) {
        let stretch = this.CalculateStretch(view, verbose, shadowsClipping, targetBackground);
        let stf = [
            // m, c0, c1, r0, r1
            [stretch.m, stretch.c0, stretch.c1, 0, 1],
            [stretch.m, stretch.c0, stretch.c1, 0, 1],
            [stretch.m, stretch.c0, stretch.c1, 0, 1],
            [0, 1, 0.5, 0, 1]
        ];

        if (verbose) {
            console.writeln("<end><cbr/><br/><b>", view.fullId, "</b>:");
            console.writeln(format("c0 = %.6f", stf[0][1]));
            console.writeln(format("m  = %.6f", stf[0][0]));
            console.writeln(format("c1 = %.6f", stf[0][2]));
            console.writeln("<end><cbr/><br/>");
        }

        view.stf = stf;
    }

    this.HardApply = function (view, verbose, shadowsClipping, targetBackground) {
        let stretch = this.CalculateStretch(view, verbose, shadowsClipping, targetBackground);

        if (stretch.c0 > 0 || stretch.m != 0.5 || stretch.c1 != 1) // if not an identity transformation
        {
            let HT = new HistogramTransformation;
            HT.H = [[0, 0.5, 1, 0, 1],
            [0, 0.5, 1, 0, 1],
            [0, 0.5, 1, 0, 1],
            [stretch.c0, stretch.m, stretch.c1, 0, 1],
            [0, 0.5, 1, 0, 1]];

            HT.executeOn(view, false); // no swap file
        }
    }
};