#load "../BERTTokenizer.fs"

let sampleText = """Unos has been around for ever, & I feel like this restaurant chain peak in popularity in the 80's. Honestly the decor inside still kind of looks 80s to me even though its nice with sleek booth and exposed brick.\n\nIf you haven't died and he recently I ordered you to come back and have a meal here again because honestly the food is really quite good!\n\nThey have the best chicken salad wrap ever! I love that rap so much I want agreed to walk the south side River Trail from where the Steelers practice all the way to the damn waterfront just because I knew that I could convince my boyfriend to go to Unos with me for lunch. Full disclosure: I made him call is a cab and we took a taxi back to the parking lot after lunch.\n\nListen... The food and pizza and service are very good, surprisingly so! I don't know why this place is not busier but next time you're down at the Waterfront please do consider dining here!"""

let vocabFile = @"C:\s\hack\small_bert_bert_uncased_L-2_H-128_A-2_2\assets\vocab.txt"
let vocab = BERTTokenizer.Vocabulary.loadFromFile vocabFile

let ftrs = BERTTokenizer.Featurizer.toFeatures vocab true 512 sampleText ""



