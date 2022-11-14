function noise_evaluation() {
    let guid = jsArguments[0].substring(1, jsArguments[0].length - 1);

    let fileToEvaluate = jsArguments[1];
    let isBayered = jsArguments[2].toLowerCase() === 'true';
    let outputDir = jsArguments[3];

    console.writeln("guid = " + guid);
    console.writeln("fileToEvaluate = " + fileToEvaluate);
    console.writeln("isBayered = " + isBayered);
    console.writeln("outputDir = " + outputDir);

    let fileToEvaluateFileInfo = new FileInfo(fileToEvaluate);
    if (fileToEvaluateFileInfo.isFile) {


        let window = ImageWindow.open(fileToEvaluate);
        let image = window[0].mainView.image;

        image.selectedRect = new Rect(image.width * 0.1, image.height * 0.1, image.width * 0.9, image.height * 0.9);

        if (isBayered) {
            console.writeln("Converting BayerCFAToFourChannel");
            image = extractLuminance(window[0].mainView);
        }

        let noiseEval = new ScaledNoiseEvaluation(image);

        let noiseSigma = noiseEval.sigma;
        let noisePct = 100 * noiseEval.count / image.selectedRect.area;

        window[0].forceClose();

        let filecontent = noiseSigma.toString() + '|' + noisePct.toString();
        console.writeln("Writing textfile: " + outputDir + "/" + guid + "_" + "done_noise_evaluation.txt - " + filecontent);
        File.writeTextFile(outputDir + "/" + guid + "_" + "done_noise_evaluation.txt", filecontent);

    } else {
        console.writeln("Input file does not seem to exist");

        console.writeln("Writing textfile: " + outputDir + "/" + guid + "_" + "error_noise_evaluation.txt");
        File.writeTextFile(outputDir + "/" + guid + "_" + "error_noise_evaluation.txt", "error");
    }
}

function extractLuminance(view) {
    var P = new ChannelExtraction;
    P.colorSpace = ChannelExtraction.prototype.CIELab;
    P.channels = [ // enabled, id
        [true, ""],
        [false, ""],
        [false, ""]
    ];
    P.sampleFormat = ChannelExtraction.prototype.SameAsSource;
    P.executeOn(view);
    return ImageWindow.activeWindow.currentView.image;
}

function BayerCFAToFourChannel(image) {
    let w = image.width;
    let h = image.height;
    let w2 = w >> 1;
    let h2 = h >> 1;
    let rgb = new Image(w2, h2, 4);
    for (let y = 0, j = 0; y < h; y += 2, ++j)
        for (let x = 0, i = 0; x < w; x += 2, ++i) {
            rgb.setSample(image.sample(x, y), i, j, 0);
            rgb.setSample(image.sample(x + 1, y), i, j, 1);
            rgb.setSample(image.sample(x, y + 1), i, j, 2);
            rgb.setSample(image.sample(x + 1, y + 1), i, j, 3);
        }
    return rgb;
}

function ScaledNoiseEvaluation(image) {
    let scale = image.Sn();
    if (1 + scale == 1) {
        console.writeln("Zero or insignificant data.");
        this.sigma = NaN;
        this.count = NaN;
        this.layers = NaN;
        return;
    }

    let a, n = 4, m = 0.01 * image.selectedRect.area;
    for (; ;) {
        a = image.noiseMRS(n);
        if (a[1] >= m)
            break;
        if (--n == 1) {
            console.writeln("<end><cbr>** Warning: No convergence in MRS noise evaluation routine - using k-sigma noise estimate.");
            a = image.noiseKSigma();
            break;
        }
    }
    this.sigma = a[0] / scale; // estimated scaled stddev of Gaussian noise
    this.count = a[1];       // number of pixels in the noisy pixels set
    this.layers = n;         // number of layers used for noise evaluation
}

noise_evaluation();