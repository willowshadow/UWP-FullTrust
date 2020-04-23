from keras.preprocessing import sequence
import pandas as pd
from sklearn.model_selection import train_test_split
from sklearn.feature_extraction.text import CountVectorizer
from keras.preprocessing.text import Tokenizer
from keras.preprocessing.sequence import pad_sequences
from keras.layers.convolutional import Conv1D
from keras.layers.convolutional import MaxPooling1D
from keras.models import Sequential
from keras.layers import LeakyReLU
from keras.layers import Dense, Embedding, LSTM,SpatialDropout1D
from keras.optimizers import Adam
from keras.callbacks import EarlyStopping
from sklearn.model_selection import train_test_split
from keras.utils.np_utils import to_categorical
import re
from nltk.corpus import stopwords
import numpy as np
import matplotlib.pyplot as plt


Data=pd.read_csv("hate_speech.csv")
# split into input (X) and output (Y) variables

print(Data['label'].value_counts())

df = Data.reset_index(drop=True)
REPLACE_BY_SPACE_RE = re.compile('[/(){}\[\]\|@,;]')
BAD_SYMBOLS_RE = re.compile('[^0-9a-z #+_]')
STOPWORDS = set(stopwords.words('english'))

def clean_text(text):
    """
        text: a string
        
        return: modified initial string
    """
    text = text.lower() # lowercase text
    text = REPLACE_BY_SPACE_RE.sub(' ', text) # replace REPLACE_BY_SPACE_RE symbols by space in text. substitute the matched string in REPLACE_BY_SPACE_RE with space.
    text = BAD_SYMBOLS_RE.sub('', text) # remove symbols which are in BAD_SYMBOLS_RE from text. substitute the matched string in BAD_SYMBOLS_RE with nothing. 
    text = text.replace('x', '')
#    text = re.sub(r'\W+', '', text)
    text = ' '.join(word for word in text.split() if word not in STOPWORDS) # remove stopwors from text
    return text
df['post'] = df['post'].apply(clean_text)


# The maximum number of words to be used. (most frequent)
MAX_NB_WORDS = 2000
# Max number of words in each complaint.
MAX_SEQUENCE_LENGTH = 100
# This is fixed.
EMBEDDING_DIM = 100
tokenizer = Tokenizer(num_words=MAX_NB_WORDS, filters='!"#$%&()*+,-./:;<=>?@[\]^_`{|}~', lower=True)
tokenizer.fit_on_texts(df['post'].values)
word_index = tokenizer.word_index
print('Found %s unique tokens.' % len(word_index))

X = tokenizer.texts_to_sequences(df['post'].values)
X = pad_sequences(X, maxlen=MAX_SEQUENCE_LENGTH)
print('Shape of data tensor:', X.shape)

Y = to_categorical(df['label'])
print('Shape of label tensor:', Y.shape)

X_train, X_test, Y_train, Y_test = train_test_split(X,Y, test_size = 0.10, random_state = 32)
print(X_train.shape,Y_train.shape)
print(X_test.shape,Y_test.shape)

#model = Sequential()
#model.add(Embedding(MAX_NB_WORDS, EMBEDDING_DIM, input_length=X.shape[1]))
#model.add(SpatialDropout1D(0.2))
#model.add(LSTM(50, dropout=0.2, recurrent_dropout=0.2,return_sequences=True))

#model.add(LeakyReLU(alpha=0.01))

#model.add(LSTM(40,dropout=0.2,recurrent_dropout=0.2, return_sequences=True))

#model.add(LeakyReLU(alpha=0.01))

#model.add(LSTM(30,dropout=0.2,recurrent_dropout=0.2))
#model.add(Dense(1, activation='softmax'))

embedding_vecor_length = 64
model = Sequential()
model.add(Embedding(MAX_NB_WORDS, embedding_vecor_length, input_length=MAX_SEQUENCE_LENGTH))
model.add(Conv1D(filters=16, kernel_size=3, padding='same', activation='relu'))
model.add(MaxPooling1D(pool_size=4))
model.add(LSTM(32))
model.add(Dense(2, activation='softmax'))
model.compile(loss='binary_crossentropy', optimizer=Adam(lr=0.0001), metrics=['binary_accuracy', 'categorical_accuracy'])
print(model.summary())


#model.add(LeakyReLU(alpha=0.05))



epochs = 25
batch_size = 64

history = model.fit(X_train, Y_train, epochs=epochs, batch_size=batch_size,validation_split=0.1,callbacks=[EarlyStopping(monitor='val_loss', patience=5, min_delta=0.0001)])


accr = model.evaluate(X_test,Y_test,use_multiprocessing=True)
print('Test set\n  Loss: {:0.3f}\n  Accuracy: {:0.3f}'.format(accr[0],accr[1]))



plt.title('Accuracy')
#plt.plot(history.history['accuracy'], label='train')
#plt.plot(history.history['val_accuracy'], label='test')
#plt.legend()
#plt.show()

model.save("Model.h5",overwrite=True,include_optimizer=True)


# below manual test gives 0 always despite changing text to negative sentiment
new_complaint = ['This man is an asshole, screw him.']
seq = tokenizer.texts_to_sequences(new_complaint)
padded = pad_sequences(seq, maxlen=MAX_SEQUENCE_LENGTH)
pred = model.predict_classes(padded)

temp = sum(Y == pred)

print(temp/len(Y))

labels = ['Not hate',' Hate']
print(pred, labels[np.argmax(pred)])

