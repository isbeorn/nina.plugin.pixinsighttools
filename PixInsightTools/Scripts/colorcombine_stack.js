function colorcombine() {

    let r = jsArguments[0];
    let g = jsArguments[1];
    let b = jsArguments[2];
    let outputFile = jsArguments[3];
    let outputDir = jsArguments[4];

    console.writeln("r = " + r);
    console.writeln("g = " + g);
    console.writeln("b = " + b);
    console.writeln("outputFile = " + outputFile);
    console.writeln("outputDir = " + outputDir);


    let redWindow = ImageWindow.open(r);
    let greenWindow = ImageWindow.open(g);
    let blueWindow = ImageWindow.open(b);

    let redId = redWindow[0].mainView.id;

    alignOnView(redId, greenWindow[0].mainView);
    let greenAlignWindow = ImageWindow.activeWindow;
    let greenId = greenAlignWindow.mainView.id;

    alignOnView(redId, blueWindow[0].mainView);
    let blueAlignWindow = ImageWindow.activeWindow;
    let blueId = blueAlignWindow.mainView.id;

    let P = new PixelMath;
    P.expression = redId;
    P.expression1 = greenId;
    P.expression2 = blueId;
    P.expression3 = "";
    P.useSingleExpression = false;
    P.symbols = "";
    P.clearImageCacheAndExit = false;
    P.cacheGeneratedImages = false;
    P.generateOutput = true;
    P.singleThreaded = false;
    P.optimization = true;
    P.use64BitWorkingImage = false;
    P.rescale = false;
    P.rescaleLower = 0;
    P.rescaleUpper = 1;
    P.truncate = true;
    P.truncateLower = 0;
    P.truncateUpper = 1;
    P.createNewImage = true;
    P.showNewImage = true;
    P.newImageId = "color_rgb";
    P.newImageWidth = 0;
    P.newImageHeight = 0;
    P.newImageAlpha = false;
    P.newImageColorSpace = PixelMath.prototype.RGB;
    P.newImageSampleFormat = PixelMath.prototype.SameAsTarget;
    P.noGUIMessages = true;
    /*
     * Read-only properties
     *
    P.outputData = [ // globalletiableId, globalletiableRK, globalletiableG, globalletiableB
    ];
     */

    P.executeOn(redWindow[0].mainView);

    let colorWindow = ImageWindow.activeWindow;
    redWindow[0].forceClose();
    greenWindow[0].forceClose();
    blueWindow[0].forceClose();
    greenAlignWindow.forceClose();
    blueAlignWindow.forceClose();

    colorWindow.saveAs(outputFile, false, false, false, false);
    colorWindow.forceClose();
    File.writeTextFile(outputDir + "/done_colorcombine.txt", "done");
}

colorcombine();



function alignOnView(referenceViewId, alignView) {
    let P = new StarAlignment;
    P.structureLayers = 5;
    P.noiseLayers = 0;
    P.hotPixelFilterRadius = 1;
    P.noiseReductionFilterRadius = 0;
    P.minStructureSize = 0;
    P.sensitivity = 0.100;
    P.peakResponse = 0.80;
    P.maxStarDistortion = 0.500;
    P.upperLimit = 1.000;
    P.invert = false;
    P.distortionModel = "";
    P.undistortedReference = false;
    P.distortionCorrection = false;
    P.distortionMaxIterations = 20;
    P.distortionTolerance = 0.005;
    P.distortionAmplitude = 2;
    P.localDistortion = true;
    P.localDistortionScale = 256;
    P.localDistortionTolerance = 0.050;
    P.localDistortionRejection = 2.50;
    P.localDistortionRejectionWindow = 64;
    P.localDistortionRegularization = 0.010;
    P.matcherTolerance = 0.0500;
    P.ransacTolerance = 2.00;
    P.ransacMaxIterations = 2000;
    P.ransacMaximizeInliers = 1.00;
    P.ransacMaximizeOverlapping = 1.00;
    P.ransacMaximizeRegularity = 1.00;
    P.ransacMinimizeError = 1.00;
    P.maxStars = 0;
    P.fitPSF = StarAlignment.prototype.FitPSF_DistortionOnly;
    P.psfTolerance = 0.50;
    P.useTriangles = false;
    P.polygonSides = 5;
    P.descriptorsPerStar = 20;
    P.restrictToPreviews = true;
    P.intersection = StarAlignment.prototype.MosaicOnly;
    P.useBrightnessRelations = false;
    P.useScaleDifferences = false;
    P.scaleTolerance = 0.100;
    P.referenceImage = referenceViewId;
    P.referenceIsFile = false;
    P.targets = [ // enabled, isFile, image
    ];
    P.inputHints = "";
    P.outputHints = "";
    P.mode = StarAlignment.prototype.RegisterMatch;
    P.writeKeywords = true;
    P.generateMasks = false;
    P.generateDrizzleData = false;
    P.generateDistortionMaps = false;
    P.frameAdaptation = false;
    P.randomizeMosaic = false;
    P.noGUIMessages = true;
    P.useSurfaceSplines = false;
    P.extrapolateLocalDistortion = true;
    P.splineSmoothness = 0.050;
    P.pixelInterpolation = StarAlignment.prototype.Auto;
    P.clampingThreshold = 0.30;
    P.outputDirectory = "";
    P.outputExtension = ".xisf";
    P.outputPrefix = "";
    P.outputPostfix = "_r";
    P.maskPostfix = "_m";
    P.distortionMapPostfix = "_dm";
    P.outputSampleFormat = StarAlignment.prototype.SameAsTarget;
    P.overwriteExistingFiles = false;
    P.onError = StarAlignment.prototype.Continue;
    P.useFileThreads = true;
    P.fileThreadOverload = 1.00;
    P.maxFileReadThreads = 0;
    P.maxFileWriteThreads = 0;
    P.memoryLoadControl = true;
    P.memoryLoadLimit = 0.85;
    /*
     * Read-only properties
     *
    P.outputData = [ // outputImage, outputMask, totalPairMatches, inliers, overlapping, regularity, quality, rmsError, rmsErrorDev, peakErrorX, peakErrorY, H11, H12, H13, H21, H22, H23, H31, H32, H33, frameAdaptationBiasRK, frameAdaptationBiasG, frameAdaptationBiasB, frameAdaptationSlopeRK, frameAdaptationSlopeG, frameAdaptationSlopeB, frameAdaptationAvgDevRK, frameAdaptationAvgDevG, frameAdaptationAvgDevB, referenceStarX, referenceStarY, targetStarX, targetStarY, totalLocalDistortionMatches, referenceLocalDistortionX, referenceLocalDistortionY, targetLocalDistortionX, targetLocalDistortionY, targetLocalDistortionDX, targetLocalDistortionDY, localDistortionWeights, outputDistortionMap, localDistortionMinDeltaX, localDistortionMinDeltaY, localDistortionMaxDeltaX, localDistortionMaxDeltaY
    ];
     */
    P.executeOn(alignView);
}