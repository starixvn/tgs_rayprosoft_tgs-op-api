import {
    AbortError,
    HttpClient,
    HttpRequest,
    HttpResponse,
} from "@microsoft/signalr";
import { AxiosInstance } from "axios"

export class AxiosHttpClient extends HttpClient {
    public constructor(private http: AxiosInstance) {
        super();
    }

    send(request: HttpRequest): Promise<HttpResponse> {
        // Check that abort was not signaled before calling send
        if (request.abortSignal && request.abortSignal.aborted) {
            return Promise.reject(new AbortError());
        }

        if (!request.method) {
            return Promise.reject(new Error("No method defined."));
        }
        if (!request.url) {
            return Promise.reject(new Error("No url defined."));
        }

        return this.http
            .request({
                url: request.url,
                method: request.method,
                data: request.content,
                headers: {
                    ...request.headers
                }
            })
            .then(res => new HttpResponse(res.status, res.statusText, JSON.stringify(res.data)));
    }
}