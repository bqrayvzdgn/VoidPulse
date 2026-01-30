export interface SavedFilterResponse {
  id: string;
  name: string;
  filterJson: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSavedFilterRequest {
  name: string;
  filterJson: string;
}

export interface UpdateSavedFilterRequest {
  name?: string;
  filterJson?: string;
}
