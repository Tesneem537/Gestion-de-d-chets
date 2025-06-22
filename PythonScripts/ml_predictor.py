import pandas as pd
from sklearn.linear_model import LinearRegression
from sklearn.cluster import KMeans
from sklearn.preprocessing import StandardScaler
import json
import sys
from io import StringIO
import traceback
import warnings

def suppress_warnings():
    warnings.filterwarnings("ignore", category=UserWarning)
    warnings.filterwarnings("ignore", category=FutureWarning)

def main():
    suppress_warnings()
    try:
        input_json = sys.stdin.read()
        
        result = {
            "PredictedNextWeekQuantity": 0,
            "Trend": "",
            "HotelClusters": [],
            "ClusterSummary": {},
            "Message": "No data provided"
        }

        if not input_json.strip():
            return result

        data = pd.read_json(StringIO(input_json))

        if len(data) == 0:
            return result

        data['WeekNumber'] = data['WeekNumber'].astype(int)
        grouped = data.groupby(['HotelId', 'WeekNumber'])['TotalQuantity'].sum().reset_index()

        if len(grouped) > 1:
            model = LinearRegression()
            X = grouped[['WeekNumber']].rename(columns={'WeekNumber': 'week'})
            y = grouped['TotalQuantity']
            model.fit(X, y)
            
            next_week = grouped['WeekNumber'].max() + 1
            prediction = model.predict(pd.DataFrame({'week': [next_week]}))[0]

            slope = model.coef_[0]
            if slope > 0.5:
                result["Trend"] = "increasing"
            elif slope < -0.5:
                result["Trend"] = "decreasing"
            else:
                result["Trend"] = "stable"
        else:
            prediction = grouped['TotalQuantity'].mean() if len(grouped) == 1 else 0
            result["Trend"] = "not enough data"

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

            def get_label(cluster_id):
                labels = ["Low Waste", "Medium Waste", "High Waste"]
                return labels[cluster_id] if cluster_id < len(labels) else "Normal"

            hotel_clusters = []
            summary = {}

            for _, row in hotel_avg.iterrows():
                cluster = int(row['Cluster'])
                label = get_label(cluster)
                hotel_clusters.append({
                    "HotelId": int(row['HotelId']),
                    "HotelName": str(row['HotelName']),
                    "TotalQuantity": float(row['TotalQuantity']),
                    "Cluster": cluster,
                    "ClusterLabel": label
                })
                summary[cluster] = summary.get(cluster, 0) + 1

            result["HotelClusters"] = hotel_clusters
            result["ClusterSummary"] = summary

        return result

    except Exception as e:
        result["Message"] = f"Error: {str(e)}"
        return result

if __name__ == "__main__":
    try:
        result = main()
        print(json.dumps(result))
    except Exception as e:
        error_result = {
            "PredictedNextWeekQuantity": 0,
            "Trend": "error",
            "HotelClusters": [],
            "ClusterSummary": {},
            "Message": f"Critical error: {str(e)}"
        }
        print(json.dumps(error_result))
        sys.stderr.write(traceback.format_exc())