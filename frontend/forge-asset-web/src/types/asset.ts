export type AssetStatus = 'normal' | 'repair' | 'inactive'

export interface Asset {
  id: number
  assetCode: string
  name: string
  category: string | null
  brand: string | null
  model: string | null
  serialNumber: string | null
  location: string | null
  owner: string | null
  purchaseDate: string | null
  remark: string | null
  status: AssetStatus
  createdAt: string
  updatedAt: string
  deletedAt: string | null
}

export interface AssetFormInput {
  assetCode: string
  name: string
  category: string
  brand: string
  model: string
  serialNumber: string
  location: string
  owner: string
  purchaseDate: string | null
  remark: string
  status: AssetStatus
}

export interface GenerateAssetCodeResponse {
  assetCode: string
}
