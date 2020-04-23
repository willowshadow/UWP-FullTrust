import os
#import tensorflow as tf
#tf.config.set_visible_devices([], 'CPU')
os.environ['TF_CPP_MIN_LOG_LEVEL'] = '0'
#os.environ["CUDA_DEVICE_ORDER"] = "PCI_BUS_ID"   # see issue #152
os.environ["CUDA_VISIBLE_DEVICES"] = "-1"

from keras.models import load_model
from keras.preprocessing.text import Tokenizer
from keras.preprocessing.sequence import pad_sequences
import numpy as np
from nltk.corpus import stopwords
from nltk.stem import SnowballStemmer
#import HS_Model
import re
import sys
import sqlite3
import pickle
import nltk
from nltk.corpus import stopwords
import nltk.stem as Stem




myarg1= sys.argv[1]
print(sys.argv[1])
REPLACE_BY_SPACE_RE = re.compile('[/(){}\[\]\|@,;]')
BAD_SYMBOLS_RE = re.compile('[^0-9a-z #+_]')
STOPWORDS = nltk.corpus.stopwords.words('english')
STOPWORDS.extend(['nigga','bitch'])

def clean_text(text):
    """
        text: a string
        
        return: modified initial string
    """
    text = clean_tweet(text)
    text = text.lower() # lowercase text
    text = REPLACE_BY_SPACE_RE.sub(' ', text) # replace REPLACE_BY_SPACE_RE symbols by space in text. substitute the matched string in REPLACE_BY_SPACE_RE with space.
    text = BAD_SYMBOLS_RE.sub(' ', text) # remove symbols which are in BAD_SYMBOLS_RE from text. substitute the matched string in BAD_SYMBOLS_RE with nothing. 
    text = text.replace('rt', ' ')
    text = re.sub(r'\d+', '', text)
    text = text.replace('&#', ' ')
    text = re.sub(r'\W+', ' ', text)
    #text = ' '.join(word for word in text.split() if word not in STOPWORDS) # remove stopwors from text
    text = text.split()
    lemm = Stem.WordNetLemmatizer()
    lemm_words = [lemm.lemmatize(word) for word in text]
    text = " ".join(lemm_words)
    return text

def clean_tweet(tweet): 
    ''' 
        Utility function to clean tweet text by removing links, special characters 
        using simple regex statements. 
        '''
    tweet =' '.join(re.sub("(@[A-Za-z0-9_]+)|([^0-9A-Za-z \t])|(\w+:\/\/\S+)", " ", tweet).split()) 

    return tweet

new_complaint = 'Trump is such a nigga'

#newnew_complaint= clean_text(new_complaint)

model = load_model('Model-2.h5',compile=True)

with open('tokenizer.pickle', 'rb') as handle:
    tokenizer = pickle.load(handle)
##model.summary()
#import onnxmltools

## Update the input name and path for your Keras model
#input_keras_model = 'Model.h5'

## Change this path to the output name and path for the ONNX model

#output_onnx_model = 'model.onnx'

## Load your Keras model
#keras_model = load_model(input_keras_model)

## Convert the Keras model into ONNX
#onnx_model = onnxmltools.convert_keras(keras_model)

## Save as protobuf
#onnxmltools.utils.save_model(onnx_model, output_onnx_model)

#seq = tokenizer.texts_to_sequences([new_complaint])
#padded = pad_sequences(seq, maxlen=100)
#pred = model.predict(padded,batch_size=64,verbose=1)
#labels = ['Not Hate','Hate']
#print(pred, labels[np.argmax(pred)])

#seq = tokenizer.texts_to_sequences(["What the fuck is wrong with trump he ssucks"])
#padded = pad_sequences(seq, maxlen=100)
#pred = model.predict(padded,batch_size=64,verbose=1)
#labels = ['Not Hate','Hate']
#print(pred, labels[np.argmax(pred)])

#seq = tokenizer.texts_to_sequences([myarg1])
#padded = pad_sequences(seq, maxlen=100)
#pred = model.predict(padded,batch_size=64,verbose=1)
#labels = ['Not Hate','Hate']
#print(pred, labels[np.argmax(pred)])

#f= open("guru99.txt","w+")

#pred=str(pred)
#for i in range(2):
#     f.write(pred % (i+1))


     
#with open('out.txt', 'w') as f:
#     print('Filename:', filename, file=f)






#+++++++++++++++++++++++++++++++++++++++++++++++++++++++++

#=====================================================
def create_connection(db_file):
    """ create a database connection to the SQLite database
        specified by the db_file
    :param db_file: database file
    :return: Connection object or None
    """
    conn = None
    try:
        conn = sqlite3.connect(db_file)
    except Error as e:
        print(e)
 
    return conn


def update_task(conn, task):
    """
    update priority, begin_date, and end date of a task
    :param conn:
    :param task:
    :return: project id
    """
    sql = ''' UPDATE Tweets
              SET pred = ? 
              WHERE _id = ?'''
    cur = conn.cursor()
    cur.execute(sql, task)
    conn.commit()

def update_task2(conn, task):
    """
    update priority, begin_date, and end date of a task
    :param conn:
    :param task:
    :return: project id
    """
    sql = ''' UPDATE Tweets
              SET Class = ? 
              WHERE _id = ?'''
    cur = conn.cursor()
    cur.execute(sql, task)
    conn.commit()


def predictor(text):
    twt=clean_text(text)
    print(twt)
    #twt = ['keep up the good work nigga ']
    #vectorizing the tweet by the pre-fitted tokenizer instance
    twt = tokenizer.texts_to_sequences([twt])
    print(twt)
    #padding the tweet to have exactly the same shape as `embedding_2` input
    twt = pad_sequences(twt, maxlen=32, padding='post')
    print(twt)
    sentiment = model.predict_proba(twt,batch_size=1,verbose = 1)
    classes = model.predict_classes(twt,batch_size=1,verbose = 1)
    print(sentiment[0][0])
    print(classes[0][0])

    return (((sentiment[0][0])*100)),int(classes[0][0])

database = myarg1
 

    # create a database connection
conn = create_connection(database)


cursor = conn.cursor()
print("Connected to SQLite")

cursor = cursor.execute('select * from Tweets;')
i=len(cursor.fetchall())
print(i,"rows")

while i>0:
    sqlite_select_query = """SELECT * from Tweets where _id = ?"""
    cursor.execute(sqlite_select_query, (i,))
    print("Reading single row \n")
    record = cursor.fetchone()
    #print(record)
    #print("Id: ", record[0])
    #print("text: ", record[1])
    #print("pred: ", record[2])
    Prediction,Classvar=predictor(record[1])
    update_task(conn,(Prediction,i))
    update_task2(conn,(Classvar,i))
    #print("prediction: ", record[3])
    i-=1

cursor.close()

#with conn
#update_task(conn, (95,1))
