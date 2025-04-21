import pandas as pd
from sklearn.linear_model import LinearRegression
from sklearn.cluster import KMeans
import json
import sys
from io import StringIO
from sklearn.preprocessing import StandardScaler
from joblib import dump, load



scaler = StandardScaler()
scaled_values = scaler.fit_transform(hotel_avg[['TotalQuantity']])
hotel_avg['Cluster'] = kmeans.fit_predict(scaled_values)

# Fix the FutureWarning by using StringIO
input_json = sys.stdin.read()
data = pd.read_json(StringIO(input_json))

# Preprocess data
data['WeekNumber'] = data['WeekNumber'].astype(int)

# ML Model 1: Predict next week's quantity
if len(data) > 0:  # Only run if we have data
    grouped = data.groupby(['HotelId', 'WeekNumber'])['TotalQuantity'].sum().reset_index()
    
    if len(grouped) > 1:  # Need at least 2 points for linear regression
        model = LinearRegression()
        model.fit(grouped[['WeekNumber']], grouped['TotalQuantity'])
        next_week = grouped['WeekNumber'].max() + 1
        prediction = model.predict([[next_week]])[0]
    else:
        prediction = grouped['TotalQuantity'].mean() if len(grouped) == 1 else 0
else:
    prediction = 0

# ML Model 2: Cluster hotels - only if we have enough data
hotel_clusters = []
if len(data) >= 3:  # Need at least 3 hotels for 3 clusters
    hotel_avg = data.groupby('HotelId')['TotalQuantity'].mean().reset_index()
    kmeans = KMeans(n_clusters=min(3, len(hotel_avg)), random_state=42)
    hotel_avg['Cluster'] = kmeans.fit_predict(hotel_avg[['TotalQuantity']])
    
    # Include hotel names
    hotel_names = data[['HotelId', 'HotelName']].drop_duplicates()
    result_df = pd.merge(hotel_avg, hotel_names, on='HotelId')
    hotel_clusters = result_df.to_dict('records')

result = {
    "PredictedNextWeekQuantity": float(prediction),
    "HotelClusters": [
        {
            "HotelId": int(row['HotelId']),
            "HotelName": str(row['HotelName']),
            "TotalQuantity": float(row['TotalQuantity']),
            "Cluster": int(row['Cluster'])
        }
        for _, row in result_df.iterrows()
    ] if len(data) >= 3 else [],
    "Message": "Success" if len(data) > 0 else "No data provided"
}
with open("ml_log.txt", "a") as f:
    f.write(f"Prediction: {prediction}, Clusters: {hotel_clusters}\n")

# To save:
dump(model, 'model.joblib')

# To load:
model = load('model.joblib')
print(json.dumps(result))