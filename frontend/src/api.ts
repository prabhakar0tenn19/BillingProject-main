import axios from 'axios';

// Set up Axios client with base URL pointing to Render deployed backend
const api = axios.create({
  baseURL: 'https://billingproject-main.onrender.com/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Response interceptor to automatically extract response wrapper
api.interceptors.response.use(
  (response) => {
    // If the response data is a binary Blob, return it directly to avoid wrapping
    if (response.data instanceof Blob) {
      return response.data;
    }
    // If response follows the envelope structure, return the inner data
    if (response.data && response.data.success !== undefined) {
      return response.data; // returns { success, data, message }
    }
    return { success: true, data: response.data, message: null };
  },
  (error) => {
    const message = error.response?.data?.message || error.message || 'API request failed';
    return Promise.reject(new Error(message));
  }
);

// Export as any to bypass AxiosResponse type checking in page components
export default api as any;
