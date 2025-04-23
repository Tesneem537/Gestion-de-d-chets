import pandas as pd
from sklearn.linear_model import LinearRegression
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
from joblib import dump, load
import json
import sys
from io import StringIO

input_json = sys.stdin.read()
data = pd.read_json(StringIO(input_json))
result = {
    "PredictedNextWeekQuantity": 0,
    "HotelClusters": [],
    "Message": "No data provided"
}
if len(data) > 0:
 
    data['WeekNumber'] = data['WeekNumber'].astype(int)
    
    
    grouped = data.groupby(['HotelId', 'WeekNumber'])['TotalQuantity'].sum().reset_index()
    
    if len(grouped) > 1:  
        model = LinearRegression()
        model.fit(grouped[['WeekNumber']], grouped['TotalQuantity'])
        next_week = grouped['WeekNumber'].max() + 1
        prediction = model.predict([[next_week]])[0]
    else:
        prediction = grouped['TotalQuantity'].mean() if len(grouped) == 1 else 0
    
    result["PredictedNextWeekQuantity"] = float(prediction)
    result["Message"] = "Success"
    

    if len(data) >= 3:  
        hotel_avg = data.groupby('HotelId').agg({
            'TotalQuantity': 'mean',
            'HotelName': 'first'
        }).reset_index()
        
        scaler = StandardScaler()
        scaled_values = scaler.fit_transform(hotel_avg[['TotalQuantity']])
        
        n_clusters = min(3, len(hotel_avg))
        kmeans = KMeans(n_clusters=n_clusters, random_state=42)
        hotel_avg['Cluster'] = kmeans.fit_predict(scaled_values)
        
        hotel_clusters = []
        for _, row in hotel_avg.iterrows():
            hotel_clusters.append({
                "HotelId": int(row['HotelId']),
                "HotelName": str(row['HotelName']),
                "TotalQuantity": float(row['TotalQuantity']),
                "Cluster": int(row['Cluster'])
            })
        
        result["HotelClusters"] = hotel_clusters

with open("ml_log.txt", "a") as f:
    f.write(f"Prediction: {result['PredictedNextWeekQuantity']}, Clusters: {len(result['HotelClusters'])}\n")

print(json.dumps(result))