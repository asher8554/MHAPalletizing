import http.server
import socketserver
import json
import os

PORT = 8000
PACKING_RESULTS_DIR = "../Results" # Read from Results folder in parent directory

class MyHttpRequestHandler(http.server.SimpleHTTPRequestHandler):
    def guess_type(self, path):
        if path.endswith('.csv'):
            return 'text/csv'
        return super().guess_type(path)

    def do_GET(self):
        if self.path == '/list_csv':
            self.send_response(200)
            self.send_header('Content-type', 'application/json')
            self.end_headers()

            csv_files = []
            # Construct the full path to the Results directory
            script_dir = os.path.dirname(os.path.abspath(__file__))
            full_packing_results_path = os.path.join(script_dir, PACKING_RESULTS_DIR)

            if os.path.exists(full_packing_results_path) and os.path.isdir(full_packing_results_path):
                for filename in os.listdir(full_packing_results_path):
                    # Only include item_placements CSV files
                    if filename.startswith('item_placements_') and filename.endswith('.csv'):
                        csv_files.append(filename)

            self.wfile.write(json.dumps(csv_files).encode('utf-8'))
        elif self.path.startswith('/packing_data/'):
            filename = self.path.split('/')[-1]
            script_dir = os.path.dirname(os.path.abspath(__file__))
            file_path = os.path.join(script_dir, PACKING_RESULTS_DIR, filename)

            if os.path.exists(file_path) and os.path.isfile(file_path):
                self.send_response(200)
                self.send_header('Content-type', 'text/csv')
                self.end_headers()
                with open(file_path, 'rb') as f:
                    self.wfile.write(f.read())
            else:
                self.send_error(404, 'File Not Found: %s' % filename)
        else:
            super().do_GET()

Handler = MyHttpRequestHandler

with socketserver.TCPServer(("", PORT), Handler) as httpd:
    print("serving at port", PORT)
    httpd.serve_forever()
