# BERTTokenizer
A helper library to split a string into tokens suitable for consumption by BERT-based models. BERT requires that some words be split furhter into smaller tokens called 'word pieces'. 

Tokenization requires a vocabluary dictionary. The standard text BERT vocabulary file can be loaded using the following helper function:

```F#
let vocab = BERTTokenizer.Vocabulary.loadFromFile vocabFile
```
The simplest way to use the tokenizer is as follows:

```F#
let sampleText = """Unos has been around for ever, & I feel like this restaurant chain peak in popularity..."""

let contextText = ""

let vocab = BERTTokenizer.Vocabulary.loadFromFile <path>
let toLowerCase = true //for 'uncased' BERT models
let maxSeqLen   = 512

let ftrs = BERTTokenizer.Featurizer.toFeatures vocab toLowerCase maxSeqLen sampleText contextText
```
BERT Tokenizer takes two strings. Internally it will join the two strings with a separator in between and return the token sequence. This format is used for question/answer type tasks. The second string can be empty for other tasks such as text classification.

The returned 'ftrs' record contains token data, e.g token id, separator type ids, mask, etc. The data from this record can be converted to tensors for model consumption


