function debayer() {
    let guid = jsArguments[0].substring(1, jsArguments[0].length - 1);
    let fileToCalibrate = jsArguments[1].substring(1, jsArguments[1].length - 1);
    let outputDir = jsArguments[2].substring(1, jsArguments[2].length - 1);
    let bayerPattern = jsArguments[3];

    console.writeln("guid = " + guid);
    console.writeln("fileToCalibrate = " + fileToCalibrate);
    console.writeln("outputDir = " + outputDir);
    console.writeln("bayerPattern = " + bayerPattern);



    let P = new Debayer;
    P.cfaPattern = Debayer.prototype[bayerPattern];
    P.debayerMethod = Debayer.prototype.VNG;
    P.fbddNoiseReduction = 0;
    P.showImages = true;
    P.cfaSourceFilePath = "";
    P.targetItems = [ // enabled, image
        [true, fileToCalibrate]
    ];
    P.noGUIMessages = true;
    P.evaluateNoise = true;
    P.noiseEvaluationAlgorithm = Debayer.prototype.NoiseEvaluation_MRS;
    P.evaluateSignal = true;
    P.structureLayers = 5;
    P.noiseLayers = 1;
    P.hotPixelFilterRadius = 1;
    P.noiseReductionFilterRadius = 0;
    P.minStructureSize = 0;
    P.psfType = Debayer.prototype.PSFType_Moffat4;
    P.psfRejectionLimit = 5.00;
    P.maxStars = 24576;
    P.inputHints = "";
    P.outputHints = "";
    P.outputRGBImages = true;
    P.outputSeparateChannels = false;
    P.outputDirectory = outputDir;
    P.outputExtension = ".xisf";
    P.outputPrefix = "";
    P.outputPostfix = "_d";
    P.overwriteExistingFiles = false;
    P.onError = Debayer.prototype.OnError_Continue;
    P.useFileThreads = true;
    P.fileThreadOverload = 1.00;
    P.maxFileReadThreads = 0;
    P.maxFileWriteThreads = 0;
    P.memoryLoadControl = true;
    P.memoryLoadLimit = 0.85;


    P.executeGlobal();
    File.writeTextFile(outputDir + "/" + guid + "_" + "debayer.txt", "started");
}
debayer();

