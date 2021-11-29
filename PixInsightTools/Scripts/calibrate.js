function calibrate() {
    let guid = jsArguments[0].substring(1, jsArguments[0].length - 1);
    let fileToCalibrate = jsArguments[1];
    let outputDir = jsArguments[2];
    let masterDark = jsArguments[3];
    let masterFlat = jsArguments[4];
    let masterBias = jsArguments[5];
    let compress = jsArguments[6];
    let pedestal = jsArguments[7];
    let saveAs16Bit = jsArguments[8];

    fileToCalibrate = fileToCalibrate.substring(1, fileToCalibrate.length - 1);
    outputDir = outputDir.substring(1, outputDir.length - 1);
    masterDark = masterDark.substring(1, masterDark.length - 1);
    masterFlat = masterFlat.substring(1, masterFlat.length - 1);
    masterBias = masterBias.substring(1, masterBias.length - 1);

    console.writeln("guid = " + guid);
    console.writeln("fileToCalibrate = " + fileToCalibrate);
    console.writeln("outputDir = " + outputDir);
    console.writeln("masterDark = " + masterDark);
    console.writeln("masterFlat = " + masterFlat);
    console.writeln("masterBias = " + masterBias);
    console.writeln("compress = " + compress);
    console.writeln("pedestal = " + pedestal);
    console.writeln("saveAs16Bit = " + saveAs16Bit);

    let P = new ImageCalibration;
    P.targetFrames = [ // enabled, path
        [true, fileToCalibrate]
    ];
    P.enableCFA = true;
    P.cfaPattern = ImageCalibration.prototype.Auto;
    P.inputHints = "";
    P.outputHints = "";
    P.pedestal = 0;
    P.pedestalMode = ImageCalibration.prototype.Keyword;
    P.pedestalKeyword = "";
    P.overscanEnabled = false;
    P.overscanImageX0 = 0;
    P.overscanImageY0 = 0;
    P.overscanImageX1 = 0;
    P.overscanImageY1 = 0;
    P.overscanRegions = [ // enabled, sourceX0, sourceY0, sourceX1, sourceY1, targetX0, targetY0, targetX1, targetY1
        [false, 0, 0, 0, 0, 0, 0, 0, 0],
        [false, 0, 0, 0, 0, 0, 0, 0, 0],
        [false, 0, 0, 0, 0, 0, 0, 0, 0],
        [false, 0, 0, 0, 0, 0, 0, 0, 0]
    ];

    if (masterDark && masterDark != "") {
        P.masterDarkEnabled = true;
        P.masterDarkPath = masterDark;
    } else {
        P.masterDarkEnabled = false;
    }

    if (masterFlat && masterFlat != "") {
        P.masterFlatEnabled = true;
        P.masterFlatPath = masterFlat;
    } else {
        P.masterFlatEnabled = false;
    }

    if (masterBias && masterBias != "") {
        P.masterBiasEnabled = true;
        P.masterBiasPath = masterBias;
    } else {
        P.masterBiasEnabled = false;
    }

    P.inputHints = "fits-keywords normalize raw cfa signed-is-physical";
    if (compress) {
        P.outputHints = "properties fits-keywords no-embedded-data no-resolution compress-data compression-codec lz4+sh";
    } else {
        P.outputHints = "properties fits-keywords no-embedded-data no-resolution no-compress-data";
    }

    P.calibrateBias = false;
    P.calibrateDark = false;
    P.calibrateFlat = false;
    P.optimizeDarks = false;
    //P.darkOptimizationThreshold = 0.00000;
    //P.darkOptimizationLow = 3.0000;
    //P.darkOptimizationWindow = 1024;
    P.darkCFADetectionMode = ImageCalibration.prototype.DetectCFA;
    P.separateCFAFlatScalingFactors = true;
    P.flatScaleClippingFactor = 0.05;
    P.evaluateNoise = true;
    P.noiseEvaluationAlgorithm = ImageCalibration.prototype.NoiseEvaluation_MRS;
    P.outputDirectory = outputDir;
    P.outputExtension = ".xisf";
    P.outputPrefix = "";
    P.outputPostfix = "_c";
    if (saveAs16Bit) {
        P.outputSampleFormat = ImageCalibration.prototype.i16;
    } else {
        P.outputSampleFormat = ImageCalibration.prototype.f32;
    }
    
    if (pedestal) {
        P.outputPedestal = parseInt(pedestal);
    } else {
        P.outputPedestal = 0;
    }
    
    P.overwriteExistingFiles = false;
    P.onError = ImageCalibration.prototype.Continue;
    P.noGUIMessages = true;
    /*
     * Read-only properties
     *
    P.outputData = [ // outputFilePath, darkScalingFactorRK, darkScalingFactorG, darkScalingFactorB, noiseEstimateRK, noiseEstimateG, noiseEstimateB, noiseFractionRK, noiseFractionG, noiseFractionB, noiseAlgorithmRK, noiseAlgorithmG, noiseAlgorithmB
    ];
     */


    P.executeGlobal();
    File.writeTextFile(outputDir + "/" + guid + "_" + "calibrated.txt", "started");
}

calibrate();

