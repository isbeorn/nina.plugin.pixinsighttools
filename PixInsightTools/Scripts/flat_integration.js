function flat_integration() {

    var filesToIntegrate = jsArguments[0];
    var outputDir = jsArguments[1];
    var compress = jsArguments[2];
    var outputFile = jsArguments[3];

    console.writeln("filesToIntegrate = " + filesToIntegrate);
    console.writeln("outputDir = " + outputDir);
    console.writeln("compress = " + compress);
    console.writeln("outputFile = " + outputFile);

    let split = filesToIntegrate.split("|");
    let images = [];
    for (let i = 0; i < split.length; i++) {
        images.push([true, split[i], "", ""]);
    }

    var P = new ImageIntegration;
    //P.images = [ // enabled, path, drizzlePath, localNormalizationDataPath        
    //];
    P.images = images;

    P.inputHints = "";
    P.combination = ImageIntegration.prototype.Average;
    P.weightMode = ImageIntegration.prototype.DontCare;
    P.weightKeyword = "";
    P.weightScale = ImageIntegration.prototype.WeightScale_BWMV;
    P.adaptiveGridSize = 16;
    P.adaptiveNoScale = false;
    P.ignoreNoiseKeywords = false;
    P.normalization = ImageIntegration.prototype.Multiplicative;
    P.rejection = ImageIntegration.prototype.PercentileClip;
    P.rejectionNormalization = ImageIntegration.prototype.EqualizeFluxes;
    P.minMaxLow = 1;
    P.minMaxHigh = 1;
    P.pcClipLow = 0.200;
    P.pcClipHigh = 0.100;
    P.sigmaLow = 4.000;
    P.sigmaHigh = 3.000;
    P.winsorizationCutoff = 5.000;
    P.linearFitLow = 5.000;
    P.linearFitHigh = 2.500;
    P.esdOutliersFraction = 0.30;
    P.esdAlpha = 0.05;
    P.esdLowRelaxation = 1.50;
    P.ccdGain = 1.00;
    P.ccdReadNoise = 10.00;
    P.ccdScaleNoise = 0.00;
    P.clipLow = true;
    P.clipHigh = true;
    P.rangeClipLow = true;
    P.rangeLow = 0.000000;
    P.rangeClipHigh = true;
    P.rangeHigh = 0.980000;
    P.mapRangeRejection = true;
    P.reportRangeRejection = false;
    P.largeScaleClipLow = false;
    P.largeScaleClipLowProtectedLayers = 2;
    P.largeScaleClipLowGrowth = 2;
    P.largeScaleClipHigh = false;
    P.largeScaleClipHighProtectedLayers = 2;
    P.largeScaleClipHighGrowth = 2;
    P.generate64BitResult = false;
    P.generateRejectionMaps = false;
    P.generateIntegratedImage = true;
    P.generateDrizzleData = false;
    P.closePreviousImages = false;
    P.bufferSizeMB = 16;
    P.stackSizeMB = 1024;
    P.autoMemorySize = true;
    P.autoMemoryLimit = 0.75;
    P.useROI = false;
    P.roiX0 = 0;
    P.roiY0 = 0;
    P.roiX1 = 0;
    P.roiY1 = 0;
    P.useCache = true;
    P.evaluateNoise = true;
    P.mrsMinDataFraction = 0.010;
    P.subtractPedestals = true;
    P.truncateOnOutOfRange = false;
    P.noGUIMessages = true;
    P.showImages = true;
    P.useFileThreads = true;
    P.fileThreadOverload = 1.00;
    P.useBufferThreads = true;
    P.maxBufferThreads = 0;
/*
     * Read-only properties
     *
    P.integrationImageId = "";
    P.lowRejectionMapImageId = "";
    P.highRejectionMapImageId = "";
    P.slopeMapImageId = "";
    P.numberOfChannels = 0;
    P.numberOfPixels = 0;
    P.totalPixels = 0;
    P.outputRangeLow = 0;
    P.outputRangeHigh = 0;
    P.totalRejectedLowRK = 0;
    P.totalRejectedLowG = 0;
    P.totalRejectedLowB = 0;
    P.totalRejectedHighRK = 0;
    P.totalRejectedHighG = 0;
    P.totalRejectedHighB = 0;
    P.finalNoiseEstimateRK = 0.000e+00;
    P.finalNoiseEstimateG = 0.000e+00;
    P.finalNoiseEstimateB = 0.000e+00;
    P.finalScaleEstimateRK = 0.000e+00;
    P.finalScaleEstimateG = 0.000e+00;
    P.finalScaleEstimateB = 0.000e+00;
    P.finalLocationEstimateRK = 0.000e+00;
    P.finalLocationEstimateG = 0.000e+00;
    P.finalLocationEstimateB = 0.000e+00;
    P.referenceNoiseReductionRK = 0.0000;
    P.referenceNoiseReductionG = 0.0000;
    P.referenceNoiseReductionB = 0.0000;
    P.medianNoiseReductionRK = 0.0000;
    P.medianNoiseReductionG = 0.0000;
    P.medianNoiseReductionB = 0.0000;
    P.referenceSNRIncrementRK = 0.0000;
    P.referenceSNRIncrementG = 0.0000;
    P.referenceSNRIncrementB = 0.0000;
    P.averageSNRIncrementRK = 0.0000;
    P.averageSNRIncrementG = 0.0000;
    P.averageSNRIncrementB = 0.0000;
    P.imageData = [ // weightRK, weightG, weightB, rejectedLowRK, rejectedLowG, rejectedLowB, rejectedHighRK, rejectedHighG, rejectedHighB
    ];
     */
    P.executeGlobal();

    var stack = ImageWindow.activeWindow;
    stack.saveAs(outputFile, false, false, false, false);
    stack.forceClose();

    File.writeTextFile(outputDir + "/done_flatintegration.txt", "done");
}

flat_integration();